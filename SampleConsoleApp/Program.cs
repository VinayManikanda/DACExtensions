﻿using System;

namespace Public.Dac.Samples.App
{
    class Program
    {
        internal enum Behavior
        {
            Usage,
            RunEndToEnd,
            FilterModel,
            FilterDeploymentSteps
        }

        static void Main(string[] args)
        {

            switch (GetBehavior(args))
            {
                case Behavior.Usage:
                    Console.WriteLine(@"Specify the action you wish to perform, for example 'ModelBuilderApp.exe RunEndToEnd'
Current actions:
[RunEndToEnd] - Runs the end to end demo that creates a model, copies to another model, and saves the model to a dacpac
[FilterModel] - Runs the end to end demo that creates a model, copies to another model, and saves the model to a dacpac
[FilterDeploymentSteps] - Runs the end to end demo that creates a model, copies to another model, and saves the model to a dacpac
[Usage] - Print this usage message
");
                    break;
                case Behavior.RunEndToEnd:
                    ModelEndToEnd.Run();
                    break;
                case Behavior.FilterModel:
                    ModelFilterExample.RunFilteringExample();
                    break;
                case Behavior.FilterDeploymentSteps:
                    // TODO implement deployment example
                    break;
            }

            Console.WriteLine("Press any key to finish");
            Console.ReadKey();
        }

        private static Behavior GetBehavior(string[] args)
        {
            Behavior behavior = Behavior.Usage;
            if (args.Length > 0)
            {
                if (MatchesBehavior(args[0], Behavior.RunEndToEnd))
                {
                    behavior = Behavior.RunEndToEnd;
                }
                if (MatchesBehavior(args[0], Behavior.FilterModel))
                {
                    behavior = Behavior.FilterModel;
                }
                else if (MatchesBehavior(args[0], Behavior.FilterDeploymentSteps))
                {
                    behavior = Behavior.FilterDeploymentSteps;
                }
            }
            return behavior;
        }

        private static bool MatchesBehavior(string name, Behavior behavior)
        {
            return string.Compare(name, behavior.ToString(), StringComparison.OrdinalIgnoreCase) == 0;
        }
    }
}