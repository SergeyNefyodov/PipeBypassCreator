using Autodesk.Revit.Attributes;
using Nice3point.Revit.Toolkit.External;
using PipeBypassCreator.Core;
using PipeBypassCreator.ViewModels;
using PipeBypassCreator.Views;

namespace PipeBypassCreator.Commands
{
    [UsedImplicitly]
    [Transaction(TransactionMode.Manual)]
    public class Command : ExternalCommand
    {
        public override void Execute()
        {
            RevitApi.Initialize(ExternalCommandData);
            var viewModel = new PipeBypassCreatorViewModel();
            var view = new PipeBypassCreatorView(viewModel);
            viewModel.CloseRequest += (s, e) => view.Close();
            viewModel.HideRequest += (s, e) => view.Hide();
            viewModel.ShowRequest += (s, e) => view.ShowDialog();
            view.ShowDialog();
        }
    }
}