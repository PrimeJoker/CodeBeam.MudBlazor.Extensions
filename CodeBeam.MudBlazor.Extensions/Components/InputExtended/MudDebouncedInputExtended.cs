﻿using System.Threading.Tasks;
using System.Timers;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace MudExtensions
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class MudDebouncedInputExtended<T> : MudBaseInputExtended<T>
    {
        private System.Timers.Timer? _timer;
        private double _debounceInterval;

        /// <summary>
        /// Interval to be awaited in milliseconds before changing the Text value
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.FormComponent.Behavior)]
        public double DebounceInterval
        {
            get => _debounceInterval;
            set
            {
                if (DoubleEpsilonEqualityComparer.Default.Equals(_debounceInterval, value))
                    return;
                _debounceInterval = value;
                if (_debounceInterval == 0)
                {
                    // not debounced, dispose timer if any
                    ClearTimer(suppressTick: false);
                    return;
                }
                SetTimer();
            }
        }

        /// <summary>
        /// callback to be called when the debounce interval has elapsed
        /// receives the Text as a parameter
        /// </summary>
        [Parameter] public EventCallback<string> OnDebounceIntervalElapsed { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected Task OnChange()
        {
            if (DebounceInterval > 0 && _timer != null)
            {
                _timer.Stop();
                return base.UpdateValuePropertyAsync(false);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="updateText"></param>
        /// <returns></returns>
        protected override Task UpdateValuePropertyAsync(bool updateText)
        {
            // This method is called when Value property needs to be refreshed from the current Text property, so typically because Text property has changed.
            // We want to debounce only text-input, not a value being set, so the debouncing is only done when updateText==false (because that indicates the
            // change came from a Text setter)
            if (updateText)
            {
                // we have a change coming not from the Text setter, no debouncing is needed
                return base.UpdateValuePropertyAsync(updateText);
            }
            // if debounce interval is 0 we update immediately
            if (DebounceInterval <= 0 || _timer == null)
                return base.UpdateValuePropertyAsync(updateText);
            // If a debounce interval is defined, we want to delay the update of Value property.
            _timer.Stop();
            // restart the timer while user is typing
            _timer.Start();
            return Task.CompletedTask;
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            // if input is to be debounced, makes sense to bind the change of the text to oninput
            // so we set Immediate to true
            if (DebounceInterval > 0)
                Immediate = true;
        }

        private void SetTimer()
        {
            if (_timer == null)
            {
                _timer = new System.Timers.Timer();
                _timer.Elapsed += OnTimerTick;
                _timer.AutoReset = false;
            }
            _timer.Interval = DebounceInterval;
        }

        private void OnTimerTick(object? sender, ElapsedEventArgs e)
        {
            InvokeAsync(OnTimerTickGuiThread).CatchAndLog();
        }

        private async Task OnTimerTickGuiThread()
        {
            await base.UpdateValuePropertyAsync(false);
            await OnDebounceIntervalElapsed.InvokeAsync(Text);
        }

        private void ClearTimer(bool suppressTick = false)
        {
            if (_timer == null)
                return;
            var wasEnabled = _timer.Enabled;
            _timer.Stop();
            _timer.Elapsed -= OnTimerTick;
            _timer.Dispose();
            _timer = null;
            if (wasEnabled && !suppressTick)
                OnTimerTickGuiThread().CatchAndLog();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            ClearTimer(suppressTick: true);
        }
    }
}
