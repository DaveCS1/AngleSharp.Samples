﻿namespace Samples.ViewModels
{
    using AngleSharp;
    using AngleSharp.Dom.Events;
    using OxyPlot;
    using OxyPlot.Axes;
    using OxyPlot.Series;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    public class ProfilerViewModel : BaseViewModel, IEventViewModel
    {
        readonly Stopwatch _time;
        readonly Dictionary<Object, TimeSpan> _tracker;
        readonly IBrowsingContext _context;
        readonly PlotModel _model;

        public ProfilerViewModel(IBrowsingContext context)
        {
            _time = new Stopwatch();
            _tracker = new Dictionary<Object, TimeSpan>();
            _context = context;
            _model = CreateModel();
            _context.Parsing += TrackParsing;
            _context.Parsed += TrackParsed;
            _context.Requesting += TrackRequesting;
            _context.Requested += TrackRequested;
        }

        static PlotModel CreateModel()
        {
            var model = new PlotModel { LegendPlacement = LegendPlacement.Outside };
            var valueAxis = new LinearAxis { Position = AxisPosition.Bottom, MinimumPadding = 0.1, MaximumPadding = 0.1 };
            var categoryAxis = new CategoryAxis { Position = AxisPosition.Left };
            var series = new IntervalBarSeries();
            model.Axes.Add(categoryAxis);
            model.Axes.Add(valueAxis);
            model.Series.Add(series);
            return model;
        }

        public PlotModel PlotData
        {
            get { return _model; }
        }

        public void Reset()
        {
            ((IntervalBarSeries)_model.Series[0]).Items.Clear();
            ((CategoryAxis)_model.Axes[0]).Labels.Clear();
            _time.Restart();
        }

        void AddItem(String label, OxyColor color, TimeSpan start, TimeSpan end)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                var series = _model.Series[0] as IntervalBarSeries;
                var category = _model.Axes[0] as CategoryAxis;
                category.Labels.Add((category.Labels.Count + 1).ToString());
                var begin = start.TotalMilliseconds;
                var final = end.TotalMilliseconds;
                series.Items.Add(new IntervalBarItem(begin, final, label) { Color = color });
                _model.InvalidatePlot(true);
            });
        }

        void TrackRequesting(Object sender, Event ev)
        {
            var data = ev as RequestEvent;

            if (data != null)
            {
                _tracker.Add(data.Request, _time.Elapsed);
            }
        }

        void TrackRequested(Object sender, Event ev)
        {
            var data = ev as RequestEvent;
            var start = default(TimeSpan);

            if (data != null && _tracker.TryGetValue(data.Request, out start))
            {
                var request = data.Request;
                AddItem("Request for " + request.Address.Href, OxyColors.Red, start, _time.Elapsed);
                _tracker.Remove(request);
            }
        }

        void TrackParsing(Object sender, Event ev)
        {
            var html = ev as HtmlParseEvent;
            var css = ev as CssParseEvent;

            if (html != null)
            {
                _tracker.Add(html.Document, _time.Elapsed);
            }
            else if (css != null)
            {
                _tracker.Add(css.StyleSheet, _time.Elapsed);
            }
        }

        void TrackParsed(Object sender, Event ev)
        {
            var html = ev as HtmlParseEvent;
            var css = ev as CssParseEvent;
            var start = default(TimeSpan);

            if (html != null && _tracker.TryGetValue(html.Document, out start))
            {
                var document = html.Document;
                AddItem("Parse HTML " + document.Url, OxyColors.Orange, start, _time.Elapsed);
                _tracker.Remove(document);
            }
            else if (css != null && _tracker.TryGetValue(css.StyleSheet, out start))
            {
                var styleSheet = css.StyleSheet;
                AddItem("Parse CSS " + styleSheet.Href, OxyColors.Violet, start, _time.Elapsed);
                _tracker.Remove(styleSheet);
            }
        }
    }
}
