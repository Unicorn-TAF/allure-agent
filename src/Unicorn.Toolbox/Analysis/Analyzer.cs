﻿using System;
using System.IO;
using System.Reflection;
using Unicorn.Taf.Core.Engine;
using Unicorn.Taf.Core.Testing.Tests;

namespace Unicorn.Toolbox.Analysis
{
    public class Analyzer
    {
        private readonly string assemblyFile;

        public Analyzer(string fileName)
        {
            this.assemblyFile = fileName;
            this.AssemblyFileName = Path.GetFileName(fileName);
            this.TestsAssemblyName = AssemblyName.GetAssemblyName(fileName).FullName;
            this.Data = new AutomationData();
        }

        public AutomationData Data { get; protected set; }

        public string AssemblyFileName { get; protected set; }

        public string TestsAssemblyName { get; protected set; }

        public void GetTestsStatistics()
        {
            using (var loader = new UnicornAppDomainIsolation<GetTestsStatisticsWorker>(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)))
            {
                this.Data = loader.Instance.GetTestsStatistics(assemblyFile);
            }
        }

        public class GetTestsStatisticsWorker : MarshalByRefObject
        {
            private readonly Type baseSuiteType = typeof(TestSuite);

            public AutomationData GetTestsStatistics(string assemblyPath)
            {
                var data = new AutomationData();
                var testsAssembly = Assembly.LoadFrom(assemblyPath);
                var allSuites = TestsObserver.ObserveTestSuites(testsAssembly);

                foreach (var suiteType in allSuites)
                {
                    if (AdapterUtilities.IsSuiteParameterized(suiteType))
                    {
                        foreach (var parametersSet in AdapterUtilities.GetSuiteData(suiteType))
                        {
                            var parameterizedSuite = testsAssembly.CreateInstance(suiteType.FullName, true, BindingFlags.Default, null, parametersSet.Parameters.ToArray(), null, null);
                            ((TestSuite)parameterizedSuite).Name += $" [{parametersSet.Name}]";
                            data.AddSuiteData(GetSuiteInfo(parameterizedSuite));
                        }
                    }
                    else
                    {
                        var nonParameterizedSuite = testsAssembly.CreateInstance(suiteType.FullName);
                        data.AddSuiteData(GetSuiteInfo(nonParameterizedSuite));
                    }
                }

                return data;
            }

            private SuiteInfo GetSuiteInfo(object suiteInstance)
            {
                int inheritanceCounter = 0;
                var currentType = suiteInstance.GetType();

                while (!currentType.Equals(baseSuiteType) && inheritanceCounter++ < 50)
                {
                    currentType = currentType.BaseType;
                }

                var testSuite = suiteInstance as TestSuite;
                var suiteInfo = new SuiteInfo(testSuite.Name, testSuite.Tags, testSuite.Metadata);

                var fieldInfo = currentType.GetField("tests", BindingFlags.NonPublic | BindingFlags.Instance);
                var tests = fieldInfo.GetValue(suiteInstance) as Test[];

                foreach (var test in tests)
                {
                    suiteInfo.TestsInfos.Add(GetTestInfo(test));
                }

                return suiteInfo;
            }

            private TestInfo GetTestInfo(Test test) => 
                new TestInfo(test.Outcome.Title, test.Outcome.Author, test.Categories);
        }
    }
}
