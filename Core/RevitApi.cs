﻿using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace PipeBypassCreator.Core
{
    /// <summary>
    ///     The class contains wrapping methods for working with the Revit API.
    /// </summary>
    public static class RevitApi
    {
        public static UIApplication UiApplication { get; set; }
        public static UIDocument UiDocument { get => UiApplication.ActiveUIDocument; }
        public static Document Document { get => UiDocument.Document; }

        public static void Initialize(ExternalCommandData commandData)
        {
            UiApplication = commandData.Application;
        }
    }
}