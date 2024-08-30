using Allure.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using Unicorn.Taf.Core.Testing;

namespace Unicorn.AllureAgent
{
    /// <summary>
    /// Allure listener, which handles reporting stuff for all test items.
    /// </summary>
    public partial class AllureListener
    {
        private string testGuid = null;

        internal void StartTest(SuiteMethod suiteMethod)
        {
            try
            {
                TestOutcome outcome = suiteMethod.Outcome;
                List<Label> labels = GenerateLabels(suiteMethod);

                TestResult result = new TestResult
                {
                    uuid = outcome.Id.ToString(),
                    name = outcome.Title,
                    fullName = outcome.FullMethodName,
                    labels = labels,
                    historyId = outcome.Id.ToString(),
                };

                if (!string.IsNullOrEmpty(outcome.TestCaseId))
                {
                    result.links.Add(Link.Tms("Related test case", outcome.TestCaseId));
                }

                testGuid = outcome.Id.ToString();
                AllureLifecycle.Instance.StartTestCase(testSuite.Outcome.Id.ToString(), result);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception in StartTest." + Environment.NewLine + e);
            }
        }

        internal void FinishTest(SuiteMethod suiteMethod)
        {
            try
            {
                string uuid = suiteMethod.Outcome.Id.ToString();

                AllureLifecycle.Instance.UpdateTestCase(uuid, r => r.status = GetStatus(suiteMethod.Outcome));

                if (suiteMethod.Outcome.Result == Taf.Core.Testing.Status.Failed)
                {
                    FailTest(suiteMethod.Outcome, uuid);
                }

                testGuid = null;
                AllureLifecycle.Instance.StopTestCase(uuid);
                AllureLifecycle.Instance.WriteTestCase(uuid);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception in FinishTest." + Environment.NewLine + e);
            }
        }

        internal void StartFixture(SuiteMethod suiteMethod)
        {
            try
            {
                TestOutcome outcome = suiteMethod.Outcome;

                var result = new FixtureResult
                {
                    name = outcome.Title
                };

                switch (suiteMethod.MethodType)
                {
                    case SuiteMethodType.BeforeSuite:
                    case SuiteMethodType.BeforeTest:
                        AllureLifecycle.Instance.StartBeforeFixture(
                            testSuite.Outcome.Id.ToString(), outcome.Id.ToString(), result);
                        break;
                    case SuiteMethodType.AfterSuite:
                    case SuiteMethodType.AfterTest:
                        AllureLifecycle.Instance.StartAfterFixture(
                            testSuite.Outcome.Id.ToString(), outcome.Id.ToString(), result);
                        break;
                    default:
                        break;
                }

                testGuid = outcome.Id.ToString();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception in StartFixture." + Environment.NewLine + e);
            }
        }

        internal void FinishFixture(SuiteMethod suiteMethod)
        {
            try
            {
                string uuid = suiteMethod.Outcome.Id.ToString();
                AllureLifecycle.Instance.UpdateFixture(uuid, r => r.status = GetStatus(suiteMethod.Outcome));

                if (suiteMethod.Outcome.Result == Taf.Core.Testing.Status.Failed)
                {
                    FailFixture(suiteMethod.Outcome, uuid);
                }

                testGuid = null;
                AllureLifecycle.Instance.StopFixture(uuid);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception in FinishFixture." + Environment.NewLine + e);
            }
        }

        internal void SkipTest(SuiteMethod suiteMethod)
        {
            StartTest(suiteMethod);
            FinishTest(suiteMethod);
        }

        private static void FailTest(TestOutcome outcome, string uuid)
        {
            StatusDetails details = new StatusDetails()
            {
                message = outcome.Exception.Message,
                trace = outcome.Exception.StackTrace
            };

            AllureLifecycle.Instance.UpdateTestCase(uuid, r =>
            {
                r.statusDetails = details;

                if (r.steps.Any())
                {
                    r.steps.Last().status = Allure.Commons.Status.failed;
                }

                if (outcome.Defect != null)
                {
                    r.links.Add(Link.Issue("Related defect", outcome.Defect.Id));
                }

                if (outcome.Attachments.Any())
                {
                    List<Allure.Commons.Attachment> attachments = CollectAttachments(outcome);
                    r.attachments.AddRange(attachments);
                }
            });
        }

        private static void FailFixture(TestOutcome outcome, string uuid)
        {
            var details = new StatusDetails()
            {
                message = outcome.Exception.Message,
                trace = outcome.Exception.StackTrace
            };

            AllureLifecycle.Instance.UpdateFixture(uuid, r =>
            {
                r.statusDetails = details;

                if (r.steps.Any())
                {
                    r.steps.Last().status = Allure.Commons.Status.failed;
                }

                if (outcome.Attachments.Any())
                {
                    List<Allure.Commons.Attachment> attachments = CollectAttachments(outcome);
                    r.attachments.AddRange(attachments);
                }
            });
        }

        private static List<Allure.Commons.Attachment> CollectAttachments(TestOutcome outcome)
        {
            var attachments = new List<Allure.Commons.Attachment>();

            foreach (var a in outcome.Attachments)
            {
                var attachment = new Allure.Commons.Attachment()
                {
                    name = a.Name,
                    source = a.FilePath,
                    type = a.MimeType
                };

                attachments.Add(attachment);
            }

            return attachments;
        }

        private static Allure.Commons.Status GetStatus(TestOutcome outcome)
        {
            switch (outcome.Result)
            {
                case Taf.Core.Testing.Status.Failed:
                    return GetFailedStatus();
                case Taf.Core.Testing.Status.Skipped:
                    return Allure.Commons.Status.skipped;
                default:
                    return Allure.Commons.Status.passed;
            }

            Allure.Commons.Status GetFailedStatus() =>
                outcome.Exception.GetType().Name.ToLowerInvariant().Contains("assert") ?
                Allure.Commons.Status.failed :
                Allure.Commons.Status.broken;
        }

        private List<Label> GenerateLabels(SuiteMethod suiteMethod)
        {
            TestOutcome outcome = suiteMethod.Outcome;

            List<Label> labels = new List<Label>()
                {
                    Label.Thread(),
                    Labels.LangLabel,
                    Labels.FrameworkLabel,
                    Label.Host(Environment.MachineName),
                    Label.TestClass(testSuite.GetType().Name),
                    Label.Package(testSuite.GetType().Namespace)
                };

            List<string> suites = new List<string>();

            if (suiteMethod.MethodType.Equals(SuiteMethodType.Test))
            {
                labels.Add(Label.Owner(outcome.Author));
                labels.AddRange(testSuite.Tags.Select(tag => Label.Feature(tag)));
                suites.AddRange((suiteMethod as Test).Categories);

                string testCaseId = string.IsNullOrEmpty(outcome.TestCaseId) ? "-1" : outcome.TestCaseId;
                labels.Add(new Label() { name = Labels.Names.TestCaseId, value = testCaseId });
            }

            if (!suites.Any())
            {
                suites.Add("Tests without suite");
            }

            labels.AddRange(suites.Select(s => Label.Suite(s)));

            return labels;
        }
    }
}
