using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI.Selection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using PipeBypassCreator.Core;
using PipeBypassCreator.ViewModels.Enums;
using PipeBypassCreator.ViewModels.Objects;
using System;
using System.Windows.Controls;

namespace PipeBypassCreator.ViewModels
{
    public sealed partial class PipeBypassCreatorViewModel : ObservableObject
    {
        [ObservableProperty] private bool _isHorizontal = true;
        [ObservableProperty] private bool _hasSnap;
        [ObservableProperty] private bool _isCyclic;
        [ObservableProperty] private Direction _direction;
        [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(CreateDuckCommand))] private string _offset;
        [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(CreateDuckCommand))] private string _corner;

        private Reference firstReference;
        private Reference secondReference;
        private XYZ firstPoint;
        private XYZ secondPoint;

        private Connector connector11;
        private Connector connector21;
        private Connector connector22;
        private Connector connector32;

        private Connector contr_connector11;
        private Connector contr_connector21;
        private Connector contr_connector22;
        private Connector contr_connector32;

        private ElementId pipeTypeId;
        private ElementId levelId;

        private double offset
        {
            get
            {
                if (double.TryParse(Offset, out double o)) return o.FromMillimeters();
                return 0;
            }
        }


        [RelayCommand(CanExecute = nameof(CanCreateDuck))] private void CreateDuck()
        {
            RaiseHideRequest();
            do
            {
                try
                {
                    MEPCurve m1 = null;
                    if (HasSnap)
                    {
                        m1 = SelectPointsWithSnap();
                    }
                    else
                    {
                        m1 = SelectPoints();
                    }
                    if (m1 == null)
                        continue;
                    pipeTypeId = m1.GetTypeId();
                    levelId = m1.get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM).AsElementId();
                    XYZ[] breakPoints = GetBreakPoints(m1);
                    using (TransactionGroup tg = new TransactionGroup(RevitApi.Document, "Создание обхода"))
                    {
                        tg.Start();
                        using (Transaction t = new Transaction(RevitApi.Document, "Создание обхода"))
                        {
                            t.Start();
                            MEPCurve newPipe = CreateNewPipe(m1, breakPoints);
                            t.Commit();
                            t.Start();
                            CorrectNewConnectorsByAngle();
                            t.Commit();
                            t.Start();
                            Pipe p1 = Pipe.Create(RevitApi.Document, pipeTypeId, levelId, connector11, connector21);
                            Pipe p2 = Pipe.Create(RevitApi.Document, pipeTypeId, levelId, connector22, connector32);
                            FindContrConnectors(p1, p2);
                            CreateElbows();
                            t.Commit();
                        }
                        tg.Assimilate();
                    }
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                {
                    break;
                }
            }
            while (IsCyclic);
            RaiseShowRequest();
        }

        private bool CanCreateDuck()
        {
            return int.TryParse(Offset, out _) && int.TryParse(Corner, out _);
        }

        private MEPCurve SelectPoints()
        {
            var filter = new SelectionFilter();
            firstReference = RevitApi.UiDocument.Selection.PickObject(ObjectType.Element, filter, 
                "Выберите точку на первом элементе");

            filter.Element = RevitApi.Document.GetElement(firstReference);

            secondReference = RevitApi.UiDocument.Selection.PickObject(ObjectType.Element, filter, 
                "Выберите вторую точку на том же элементе");
            return RevitApi.Document.GetElement(firstReference) as MEPCurve;
        }

        private MEPCurve SelectPointsWithSnap()
        {
            firstReference = RevitApi.UiDocument.Selection.PickObject(ObjectType.Element, new SelectionFilter(),
                "Выберите элемент для построения обхода");
            firstPoint = RevitApi.UiDocument.Selection.PickPoint((ObjectSnapTypes)1023,
                "Укажите первую точку");
            secondPoint = RevitApi.UiDocument.Selection.PickPoint((ObjectSnapTypes)1023, 
                "Укажите вторую точку");
            return RevitApi.Document.GetElement(firstReference) as MEPCurve;
        }

        private XYZ[] GetBreakPoints(MEPCurve mepCurve)
        {
            XYZ[] points = [null, null];            
            var line = (mepCurve.Location as LocationCurve).Curve as Line;
            XYZ breakPoint0 = null, breakPoint1 = null;
            if (HasSnap)
            {
                breakPoint0 = line.Project(firstPoint).XYZPoint;
                breakPoint1 = line.Project(secondPoint).XYZPoint;
            }
            else
            {
                breakPoint0 = line.Project(firstReference.GlobalPoint).XYZPoint;
                breakPoint1 = line.Project(secondReference.GlobalPoint).XYZPoint;
            }
            points[0] = breakPoint0;
            points[1] = breakPoint1;
            return points;
        }

        private MEPCurve CreateNewPipe(MEPCurve baseCurve, XYZ[] breakPoints)
        {
            var secondCurveId = PlumbingUtils.BreakCurve(RevitApi.Document, baseCurve.Id, breakPoints[0]);
            var thirdCurveId = BreakBrokenCurve(baseCurve, breakPoints, secondCurveId);
            MEPCurve newPipe = null;
            var secondCurve = RevitApi.Document.GetElement(secondCurveId) as MEPCurve;
            var thirdCurve = RevitApi.Document.GetElement(thirdCurveId) as MEPCurve;
            List<MEPCurve> mepCurves = [baseCurve, secondCurve, thirdCurve];
            newPipe = FindNewPipe(mepCurves, breakPoints);
            var i = mepCurves.IndexOf(newPipe);
            mepCurves.RemoveAt(i);
            foreach (MEPCurve curve in mepCurves)
            {
                foreach (Connector connector in curve.ConnectorManager.Connectors)
                {
                    if (connector.ConnectorType != ConnectorType.End) continue;
                    if (connector.Origin.IsAlmostEqualTo(connector21.Origin))
                    {
                        connector11 = connector;
                    }
                    else if (connector.Origin.IsAlmostEqualTo(connector22.Origin))
                    {
                        connector32 = connector;
                    }
                }
            }
            MovePipe(newPipe);
            return newPipe;
        }

        private void MovePipe(MEPCurve newPipe)
        {            
            if (IsHorizontal)
            {
                var heightParam = newPipe.get_Parameter(BuiltInParameter.RBS_OFFSET_PARAM);
                var height = heightParam.AsDouble();
                if (Direction == Direction.UpVertical)
                {
                    heightParam.Set(height + offset);
                }
                else if (Direction == Direction.DownVertical)
                {
                    heightParam.Set(height - offset);
                }
            }
            else 
            {
                if (Direction == Direction.UpHorizontal)
                {
                    ElementTransformUtils.MoveElement(RevitApi.Document, newPipe.Id, new XYZ(0, 1, 0) * offset);
                }
                else if (Direction == Direction.DownHorizontal)
                {
                    ElementTransformUtils.MoveElement(RevitApi.Document, newPipe.Id, new XYZ(0, -1, 0) * offset);
                }
                else if (Direction == Direction.Left)
                {
                    ElementTransformUtils.MoveElement(RevitApi.Document, newPipe.Id, new XYZ(-1, 0, 0) * offset);
                }
                else if (Direction == Direction.Right)
                {
                    ElementTransformUtils.MoveElement(RevitApi.Document, newPipe.Id, new XYZ(1, 0, 0) * offset);
                }
            }
        }

        private static ElementId BreakBrokenCurve(MEPCurve mepCurve, XYZ[] breakPoints, ElementId secondPipeId)
        {
            ElementId thirdPipeId;
            try
            {
                thirdPipeId = PlumbingUtils.BreakCurve(RevitApi.Document, mepCurve.Id, breakPoints[1]);
            }
            catch
            {
                thirdPipeId = PlumbingUtils.BreakCurve(RevitApi.Document, secondPipeId, breakPoints[1]);
            }

            return thirdPipeId;
        }

        private MEPCurve FindNewPipe(List<MEPCurve> mepCurves, XYZ[] breakPoints)
        {
            MEPCurve newPipe = null;
            foreach (MEPCurve curve in mepCurves)
            {
                bool a = false, b = false;
                foreach (Connector connector in curve.ConnectorManager.Connectors)
                {
                    if (connector.ConnectorType != ConnectorType.End) continue;
                    if (connector.Origin.IsAlmostEqualTo(breakPoints[0]) || connector.Origin.IsAlmostEqualTo(breakPoints[1]))
                    {
                        if (!a)
                        {
                            a = true;
                            connector21 = connector;
                        }
                        else
                        {
                            b = true;
                            connector22 = connector;
                        }
                    }
                }
                if (a && b)
                {
                    newPipe = curve;
                    break;
                }
            }
            return newPipe;
        }

        private void CorrectNewConnectorsByAngle()
        {
            double.TryParse(Corner, out double angle);
            if (angle >89.99) return;
            double alpha = angle * (Math.PI / 180);

            XYZ A = connector21.Origin;
            XYZ B = connector22.Origin;
            XYZ E = null;
            XYZ F = null;
            
            E = A + ((B - A) / B.DistanceTo(A) * offset * Math.Cos(alpha) / Math.Sin(alpha));            
            F = B + ((A - B) / B.DistanceTo(A) * offset * Math.Cos(alpha) / Math.Sin(alpha));
            connector21.Origin = E;
            connector22.Origin = F;
        }
        private void FindContrConnectors(Pipe p1, Pipe p2)
        {
            foreach (Connector c in p1.ConnectorManager.Connectors)
            {
                if (c.Origin.IsAlmostEqualTo(connector11.Origin))
                {
                    contr_connector11 = c;
                }
                else
                {
                    contr_connector21 = c;
                }
            }
            foreach (Connector c in p2.ConnectorManager.Connectors)
            {
                if (c.Origin.IsAlmostEqualTo(connector32.Origin))
                {
                    contr_connector32 = c;
                }
                else
                {
                    contr_connector22 = c;
                }
            }
        }
        private void CreateElbows()
        {
            RevitApi.Document.Create.NewElbowFitting(contr_connector11, connector11);
            RevitApi.Document.Create.NewElbowFitting(connector21, contr_connector21);
            RevitApi.Document.Create.NewElbowFitting(connector22, contr_connector22);
            RevitApi.Document.Create.NewElbowFitting(connector32, contr_connector32);
        }

        public event EventHandler CloseRequest;
        private void RaiseCloseRequest()
        {
            CloseRequest?.Invoke(this, EventArgs.Empty);
        }
        public event EventHandler HideRequest;
        private void RaiseHideRequest()
        {
            HideRequest?.Invoke(this, EventArgs.Empty);
        }
        public event EventHandler ShowRequest;
        private void RaiseShowRequest()
        {
            ShowRequest?.Invoke(this, EventArgs.Empty);
        }
    }
}