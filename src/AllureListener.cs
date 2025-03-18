using Allure.Net.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using Unicorn.Taf.Core.Testing;
using AllureStatus = Allure.Net.Commons.Status;
using AllureAttachment = Allure.Net.Commons.Attachment;

namespace Unicorn.Reporting.Allure
{
    /// <summary>
    /// Allure listener, which handles reporting stuff for all test items.
    /// </summary>
    public partial class AllureListener
    {
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
                    result.links.Add(Link.Tms("Test case: " + outcome.TestCaseId, outcome.TestCaseId));
                }

                AllureLifecycle.Instance.StartTestCase(result);
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

                AllureLifecycle.Instance.UpdateTestCase(r => r.status = GetStatus(suiteMethod.Outcome));

                if (suiteMethod.Outcome.Result == Taf.Core.Testing.Status.Failed)
                {
                    FailTest(suiteMethod.Outcome);
                }

                AllureLifecycle.Instance.StopTestCase();
                AllureLifecycle.Instance.WriteTestCase();
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
                        AllureLifecycle.Instance.StartBeforeFixture(result);
                        break;
                    case SuiteMethodType.AfterSuite:
                    case SuiteMethodType.AfterTest:
                        AllureLifecycle.Instance.StartAfterFixture(result);
                        break;
                    default:
                        break;
                }
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
                AllureLifecycle.Instance.UpdateFixture(r => r.status = GetStatus(suiteMethod.Outcome));

                if (suiteMethod.Outcome.Result == Taf.Core.Testing.Status.Failed)
                {
                    FailFixture(suiteMethod.Outcome);
                }

                AllureLifecycle.Instance.StopFixture();
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

        private static void FailTest(TestOutcome outcome)
        {
            StatusDetails details = new StatusDetails()
            {
                message = outcome.FailMessage,
                trace = outcome.FailStackTrace
            };

            AllureLifecycle.Instance.UpdateTestCase(r =>
            {
                r.statusDetails = details;

                if (r.steps.Any())
                {
                    r.steps.Last().status = AllureStatus.failed;
                }

                if (outcome.Defect != null)
                {
                    r.links.Add(Link.Issue("Defect: " + outcome.Defect.Id, outcome.Defect.Id));
                }

                if (outcome.Attachments.Any())
                {
                    List<AllureAttachment> attachments = CollectAttachments(outcome);
                    r.attachments.AddRange(attachments);
                }
            });
        }

        private static void FailFixture(TestOutcome outcome)
        {
            var details = new StatusDetails()
            {
                message = outcome.FailMessage,
                trace = outcome.FailStackTrace
            };

            AllureLifecycle.Instance.UpdateFixture(r =>
            {
                r.statusDetails = details;

                if (r.steps.Any())
                {
                    r.steps.Last().status = AllureStatus.failed;
                }

                if (outcome.Attachments.Any())
                {
                    List<AllureAttachment> attachments = CollectAttachments(outcome);
                    r.attachments.AddRange(attachments);
                }
            });
        }

        private static List<AllureAttachment> CollectAttachments(TestOutcome outcome)
        {
            var attachments = new List<AllureAttachment>();

            foreach (var a in outcome.Attachments)
            {
                var attachment = new AllureAttachment()
                {
                    name = a.Name,
                    source = a.FilePath,
                    type = a.MimeType
                };

                attachments.Add(attachment);
            }

            return attachments;
        }

        private static AllureStatus GetStatus(TestOutcome outcome)
        {
            switch (outcome.Result)
            {
                case Taf.Core.Testing.Status.Failed:
                    return GetFailedStatus();
                case Taf.Core.Testing.Status.Skipped:
                    return AllureStatus.skipped;
                default:
                    return AllureStatus.passed;
            }

            AllureStatus GetFailedStatus() =>
                IsFailedOnAssertion(outcome.FailMessage) ?
                AllureStatus.failed :
                AllureStatus.broken;
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

        private static bool IsFailedOnAssertion(string message) =>
            message.ToLowerInvariant().Contains("assertion") || (message.Contains("Expected:") && message.Contains("But:"));
    }
}
