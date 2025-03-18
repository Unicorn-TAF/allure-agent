using Unicorn.Taf.Api;
using Unicorn.Taf.Core;

namespace Unicorn.Reporting.Allure
{
    /// <summary>
    /// Allure reporter instance. Contains subscriptions to corresponding Unicorn events.
    /// </summary>
    public sealed class AllureReporter : ITestReporter
    {
        private readonly AllureListener _listener;

        /// <summary>
        /// Initializes a new instance of the <see cref="AllureReporter"/> class.<br/>
        /// Automatic subscription to all test events.
        /// </summary>
        public AllureReporter()
        {
            _listener = new AllureListener();

            TafEvents.OnTestStart += _listener.StartTest;
            TafEvents.OnTestFinish += _listener.FinishTest;
            TafEvents.OnTestSkip += _listener.SkipTest;

            TafEvents.OnSuiteMethodStart += _listener.StartFixture;
            TafEvents.OnSuiteMethodFinish += _listener.FinishFixture;

            TafEvents.OnSuiteStart += _listener.StartSuite;
            TafEvents.OnSuiteFinish += _listener.FinishSuite;

            TafEvents.OnStepStart += _listener.StartStep;
            TafEvents.OnStepFinish += _listener.FinishStep;
        }

        /// <summary>
        /// Unsubscribes from events.
        /// </summary>
        public void Dispose()
        {
            TafEvents.OnTestStart -= _listener.StartTest;
            TafEvents.OnTestFinish -= _listener.FinishTest;
            TafEvents.OnTestSkip -= _listener.SkipTest;

            TafEvents.OnSuiteMethodStart -= _listener.StartFixture;
            TafEvents.OnSuiteMethodFinish -= _listener.FinishFixture;

            TafEvents.OnSuiteStart -= _listener.StartSuite;
            TafEvents.OnSuiteFinish -= _listener.FinishSuite;

            TafEvents.OnStepStart -= _listener.StartStep;
            TafEvents.OnStepFinish -= _listener.FinishStep;
        }
    }
}
