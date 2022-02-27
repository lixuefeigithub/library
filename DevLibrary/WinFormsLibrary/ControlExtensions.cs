using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinFormLibrary
{
    public static class ControlExtensions
    {
        public static InvokeServiceMethodResult InvokeServiceMethod(this Control window,
            Action action,
            ProgressBar progressBar = null)
        {
            var result = new InvokeServiceMethodResult
            {
                IsSuccessful = false,
            };

            if (window == null)
            {
                return result;
            }

            if (action == null)
            {
                return result;
            }

            var originalEnabledValue = window.Enabled;
            var originalProgressBarVisible = progressBar?.Visible ?? false;
            window.Enabled = false;

            progressBar?.StartProgressBar();

            try
            {
                Task.Run(action).Wait();
                result.IsSuccessful = true;
                return result;
            }
            catch (Exception ex)
            {
                ShowErrorDialog(ex);
                result.Exception = ex;
                return result;
            }
            finally
            {
                window.Enabled = originalEnabledValue;

                progressBar.StopProgressBar(originalProgressBarVisible);
            }
        }

        public static RunWithErrorHandlingResult<TResult> InvokeServiceMethod<TResult>(this Control window,
            Func<TResult> action,
            ProgressBar progressBar = null,
            bool isShowMessageBoxWhenFailed = true)
        {
            var result = new RunWithErrorHandlingResult<TResult>
            {
                IsSuccessful = false,
                Result = default,
            };

            if (window == null)
            {
                return result;
            }

            if (action == null)
            {
                return result;
            }

            var originalEnabledValue = window.Enabled;
            var originalProgressBarVisible = progressBar?.Visible ?? false;
            window.Enabled = false;

            progressBar?.StartProgressBar();

            try
            {
                result.Result = Task.Run(action).Result;
                result.IsSuccessful = true;
                return result;
            }
            catch (Exception ex)
            {
                if (isShowMessageBoxWhenFailed)
                {
                    ShowErrorDialog(ex);
                }

                result.Exception = ex;
                return result;
            }
            finally
            {
                window.Enabled = originalEnabledValue;

                progressBar.StopProgressBar(originalProgressBarVisible);
            }
        }

        //public static DialogResult ShowConfirmDialogNoMockStartTime(this Control window)
        //{
        //    return MessageBox.Show("Not provided mock start time or input invalid mock start time.\r\n\r\nAre you are you want to continue with out mock time?",
        //                "Warning - No mock time",
        //                MessageBoxButtons.OKCancel,
        //                MessageBoxIcon.Warning);
        //}

        //public static DialogResult ShowConfirmMockTimeZoneNotMatch(this Control window, TimeSpan inputTimeZone, TimeSpan expectedTimeZone, string actionName)
        //{
        //    return MessageBox.Show($"The mock time zone you have provided is not is expected for Action [{actionName}].\r\nExpected Time Zone: {expectedTimeZone.ToDisplayString()}\r\nInput Mock Time Zone: {inputTimeZone.ToDisplayString()}\r\nAre you are you want to continue with this mock time zone?",
        //                "Warning - Mock time zone not match",
        //                MessageBoxButtons.OKCancel,
        //                MessageBoxIcon.Warning);
        //}

        private static void ShowErrorDialog(Exception ex)
        {
            MessageBox.Show(ex.Message, "Errors Occurred", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static void StartProgressBar(this ProgressBar progressBar)
        {
            if (progressBar != null)
            {
                progressBar.Visible = true;
                progressBar.Style = ProgressBarStyle.Marquee;
                progressBar.MarqueeAnimationSpeed = 50;
            }
        }

        private static void StopProgressBar(this ProgressBar progressBar, bool originalProgressBarVisible)
        {
            if (progressBar != null)
            {
                progressBar.Visible = originalProgressBarVisible;
                progressBar.Style = ProgressBarStyle.Continuous;
                progressBar.MarqueeAnimationSpeed = 0;
            }
        }
    }

    public class InvokeServiceMethodResult
    {
        public bool IsSuccessful { get; set; }
        public Exception Exception { get; set; }
    }

    public class RunWithErrorHandlingResult<TResult>
    {
        public bool IsSuccessful { get; set; }
        public Exception Exception { get; set; }
        public TResult Result { get; set; }
    }
}
