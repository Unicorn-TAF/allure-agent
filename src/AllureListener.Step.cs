using Allure.Net.Commons;
using System;
using System.Reflection;
using Unicorn.Taf.Core.Steps;

namespace Unicorn.Reporting.Allure
{
    /// <summary>
    /// Allure listener, which handles reporting stuff for all test items.
    /// </summary>
    public partial class AllureListener
    {
        internal void StartStep(MethodBase method, object[] arguments)
        {
            try
            {
                var result = new StepResult()
                {
                    name = StepsUtilities.GetStepInfo(method, arguments),
                    status = Status.passed
                };

                AllureLifecycle.Instance.StartStep(result);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception in StartStep '{0}'." + Environment.NewLine + e);
            }
        }

        internal void FinishStep(MethodBase method, object[] arguments)
        {
            try
            {
                AllureLifecycle.Instance.StopStep();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception in FinishStep" + Environment.NewLine + e);
            }
        }
    }
}
