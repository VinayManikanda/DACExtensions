using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Public.Dac.Samples.App
{
    internal enum Outcome
    {
        Source
        , Destination
        , SourceOnly
        , DestinationOnly
    }
    /// <summary>
    /// Basic demo class showing results of comparing Two Dacpacs and also filters the Dacpacs and removes specific schema
    /// The output includes:
    /// Any identical SQL objects which exists on both the Dacpacs but have some differences in schema/T-SQL code
    /// SQL objects which exists on the Source Dacpac only
    /// SQL objects which exists on the Destination Dacpac only
    /// </summary>
    internal class ModelCompareDacs
    {
        private static string _sourceDacpacPath;
        private static string _destinationDacPacPath;
        private static string _ModelCompareDacsPath = "ModelCompareDacs";
        private static string _compareResultsPath = "ModelCompareResults";

        private static DisposableList _trash = new DisposableList();

        private static readonly string[] _testSourceScripts = new string[]
        {
            "CREATE SCHEMA [log]",
            "CREATE TABLE [log].[l1] (l1 INT NOT NULL PRIMARY KEY)",
            "CREATE VIEW [log].[v1] AS SELECT l1 FROM [log].[l1]",

            "CREATE TABLE [dbo].[t2] (c2 INT NOT NULL)",
            "CREATE UNIQUE CLUSTERED INDEX Idx1 ON [dbo].[t2] (c2)",
            "CREATE VIEW [dbo].[v2] AS SELECT c2 FROM [dbo].[t2]",
            "CREATE TABLE [dbo].[t3] (c3 INT NOT NULL PRIMARY KEY)",
            "CREATE TABLE [dbo].[t5] (c5 INT NOT NULL PRIMARY KEY)"
        };

        private static readonly string[] _testDestinationScripts = new string[]
        {
            "CREATE SCHEMA [log]",
            "CREATE TABLE [log].[l1] (l1 INT NOT NULL PRIMARY KEY)",
            "CREATE VIEW [log].[v1] AS SELECT l1 FROM [log].[l1]",

            "CREATE TABLE [dbo].[t2] (c2 INT NOT NULL)",
            "CREATE VIEW [dbo].[v2] AS SELECT c2 FROM [dbo].[t2]",
            "CREATE TABLE [dbo].[t4] (c4 INT NOT NULL PRIMARY KEY)",
            "CREATE TABLE [dbo].[t5] (c5 VARCHAR(1) NOT NULL PRIMARY KEY)"
        };

        private static ModelTypeClass[] modelSchemaName = new ModelTypeClass[]
            {
             ModelSchema.View
            ,ModelSchema.TableValuedFunction
            ,ModelSchema.ScalarFunction
            ,ModelSchema.Index
            ,ModelSchema.DmlTrigger
            ,ModelSchema.PartitionFunction
            ,ModelSchema.PartitionScheme
            ,ModelSchema.Schema
            ,ModelSchema.Procedure
            ,ModelSchema.Table
            ,ModelSchema.TableType
            };

        public static void Run()
        {
            Directory.CreateDirectory(GetTestDir(_ModelCompareDacsPath));
            Directory.CreateDirectory(GetTestDir(_compareResultsPath));
            _sourceDacpacPath = GetTestFilePath(_ModelCompareDacsPath, "sourceDatabase.dacpac");
            DacPackageExtensions.BuildPackage(_sourceDacpacPath, CreateTestModel(_testSourceScripts), new PackageMetadata());
            _destinationDacPacPath = GetTestFilePath(_ModelCompareDacsPath, "destinationDatabase.dacpac");
            DacPackageExtensions.BuildPackage(_destinationDacPacPath, CreateTestModel(_testDestinationScripts), new PackageMetadata());

            using (TSqlModel sourcemodel = new TSqlModel(_sourceDacpacPath),
                             destmodel = new TSqlModel(_destinationDacPacPath))
            {

                // Filtering to exclude log schema
                var schemaFilter = new SchemaBasedFilter("log");
                ModelFilterer modelFilterer = new ModelFilterer(schemaFilter);
                TSqlModel smodel = modelFilterer.CreateFilteredModel(sourcemodel);
                TSqlModel dmodel = modelFilterer.CreateFilteredModel(destmodel);

                CompareTSqlModels(smodel, dmodel);
            }
        }

        private static void SaveScriptToFile(TSqlObject scriptTsqlObject, Outcome outcomeName)
        {
            string filePath = Path.Combine(Environment.CurrentDirectory, _compareResultsPath, $"{scriptTsqlObject.Name}_{outcomeName}.txt");
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.WriteLine(scriptTsqlObject.GetScript());
            }

        }

        private static void CompareTSqlModels(TSqlModel sourcemodel, TSqlModel destModel)
        {
            List<TSqlObject> destCollection = new List<TSqlObject>();

            var sourceTables = sourcemodel.GetObjects(DacQueryScopes.UserDefined, modelSchemaName).ToList();
            var destObjects = destModel.GetObjects(DacQueryScopes.UserDefined, modelSchemaName).ToList();

            foreach (var sourcet in sourceTables)
            {
                var dbObjectParts = sourcet.Name.Parts;

                var destt = destModel.GetObjects(sourcet.ObjectType, new ObjectIdentifier(dbObjectParts), DacQueryScopes.UserDefined).FirstOrDefault();

                if (destt != null)
                {

                    bool flag = destt.GetScript().Equals(sourcet.GetScript());

                    if (!destt.GetScript().Equals(sourcet.GetScript()))
                    {
                        SaveScriptToFile(sourcet, Outcome.Source);
                        SaveScriptToFile(destt, Outcome.Destination);
                    }

                    destCollection.Add(destt);

                }
                else
                {
                    SaveScriptToFile(sourcet, Outcome.SourceOnly);
                }

            }

            var tsqldestobj = destObjects.Except(destCollection);


            foreach (var t in tsqldestobj)
            {
                SaveScriptToFile(t, Outcome.DestinationOnly);
            }

        }

        private static TSqlModel CreateTestModel(params string[] Tsqlscripts)
        {
            var scripts = Tsqlscripts;
            TSqlModel model = _trash.Add(new TSqlModel(SqlServerVersion.Sql110, new TSqlModelOptions()));
            AddScriptsToModel(model, scripts);
            return model;
        }
        private static void AddScriptsToModel(TSqlModel model, IEnumerable<string> scripts)
        {
            foreach (string script in scripts)
            {
                model.AddObjects(script);
            }
        }

        private static string GetTestDir(string directoryName)
        {

            return Path.Combine(Environment.CurrentDirectory, directoryName);
        }

        private static string GetTestFilePath(string dirName, string fileName)
        {
            return Path.Combine(GetTestDir(dirName), fileName);
        }
        public static void CleanupTest()
        {
            _trash.Dispose();
        }
    }
}