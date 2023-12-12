using Nice3point.Revit.Toolkit.External;
using PipeBypassCreator.Commands;

namespace PipeBypassCreator
{
    [UsedImplicitly]
    public class Application : ExternalApplication
    {
        public override void OnStartup()
        {
            CreateRibbon();
        }

        private void CreateRibbon()
        {
            var panel = Application.CreatePanel("Commands", "PipeBypassCreator");

            var showButton = panel.AddPushButton<Command>("Execute");
            showButton.SetImage("/PipeBypassCreator;component/Resources/Icons/RibbonIcon16.png");
            showButton.SetLargeImage("/PipeBypassCreator;component/Resources/Icons/RibbonIcon32.png");
        }
    }
}