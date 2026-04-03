// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Mapping.ViewGeneration
{
    using System.Data.Entity.Core.Common.Utils;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Text;

    // <summary>
    // This class holds some configuration information for the view generation code.
    // </summary>
    internal sealed class ConfigViewGenerator : InternalBase
    {
        internal ConfigViewGenerator()
        {
            m_watch = new Stopwatch();
            m_singleWatch = new Stopwatch();
            var numEnums = Enum.GetNames(typeof(PerfType)).Length;
            m_breakdownTimes = new TimeSpan[numEnums];
            m_traceLevel = ViewGenTraceLevel.None;
            m_generateUpdateViews = false;
            StartWatch();
        }

        private ViewGenTraceLevel m_traceLevel;
        private readonly TimeSpan[] m_breakdownTimes;
        private readonly Stopwatch m_watch;

        // <summary>
        // To measure a single thing at a time.
        // </summary>
        private readonly Stopwatch m_singleWatch;

        // <summary>
        // Perf op being measured.
        // </summary>
        [SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        private PerfType m_singlePerfOp;

        private bool m_enableValidation = true;
        private bool m_generateUpdateViews = true;

        // <summary>
        // Diagnostic counter: number of extents that took the trivial-view fast-path.
        // </summary>
        private int m_trivialViewsFastPathed;
        internal int TrivialViewsFastPathed
        {
            get { return m_trivialViewsFastPathed; }
        }

        internal void IncrementTrivialViewsFastPathed()
        {
            System.Threading.Interlocked.Increment(ref m_trivialViewsFastPathed);
        }

        // <summary>
        // Diagnostic counter: number of extents that took the full QueryRewriter path.
        // </summary>
        private int m_trivialViewsFullPath;
        internal int TrivialViewsFullPath
        {
            get { return m_trivialViewsFullPath; }
        }

        internal void IncrementTrivialViewsFullPath()
        {
            System.Threading.Interlocked.Increment(ref m_trivialViewsFullPath);
        }

        // <summary>
        // If true then view generation will produce eSQL, otherwise CQTs only.
        // </summary>
        internal bool GenerateEsql { get; set; }

        // <summary>
        // Callers can set elements in this list.
        // </summary>
        internal TimeSpan[] BreakdownTimes
        {
            get { return m_breakdownTimes; }
        }

        internal ViewGenTraceLevel TraceLevel
        {
            get { return m_traceLevel; }
            set { m_traceLevel = value; }
        }

        internal bool IsValidationEnabled
        {
            get { return m_enableValidation; }
            set { m_enableValidation = value; }
        }

        internal bool GenerateUpdateViews
        {
            get { return m_generateUpdateViews; }
            set { m_generateUpdateViews = value; }
        }

        internal bool GenerateViewsForEachType { get; set; }

        // <summary>
        // Creates a lightweight copy with independent timing state (Stopwatch)
        // but the same configuration flags. Suitable for parallel view generation.
        // </summary>
        internal ConfigViewGenerator CreateCopy()
        {
            return new ConfigViewGenerator
            {
                GenerateEsql = GenerateEsql,
                GenerateViewsForEachType = GenerateViewsForEachType,
                TraceLevel = TraceLevel,
                IsValidationEnabled = IsValidationEnabled,
                GenerateUpdateViews = GenerateUpdateViews,
            };
        }

        internal bool IsViewTracing
        {
            get { return IsTraceAllowed(ViewGenTraceLevel.ViewsOnly); }
        }

        internal bool IsNormalTracing
        {
            get { return IsTraceAllowed(ViewGenTraceLevel.Normal); }
        }

        internal bool IsVerboseTracing
        {
            get { return IsTraceAllowed(ViewGenTraceLevel.Verbose); }
        }

        private void StartWatch()
        {
            m_watch.Start();
        }

        internal void StartSingleWatch(PerfType perfType)
        {
            m_singleWatch.Start();
            m_singlePerfOp = perfType;
        }

        // <summary>
        // Sets time for <paramref name="perfType" /> for the individual timer.
        // </summary>
        internal void StopSingleWatch(PerfType perfType)
        {
            Debug.Assert(m_singlePerfOp == perfType, "Started op for different activity " + m_singlePerfOp + " -- not " + perfType);
            var timeElapsed = m_singleWatch.Elapsed;
            var index = (int)perfType;
            m_singleWatch.Stop();
            m_singleWatch.Reset();
            BreakdownTimes[index] = BreakdownTimes[index].Add(timeElapsed);
        }

        // <summary>
        // Sets time for <paramref name="perfType" /> since the last call to <see cref="SetTimeForFinishedActivity" />.
        // </summary>
        internal void SetTimeForFinishedActivity(PerfType perfType)
        {
            var timeElapsed = m_watch.Elapsed;
            var index = (int)perfType;
            BreakdownTimes[index] = BreakdownTimes[index].Add(timeElapsed);
            m_watch.Reset();
            m_watch.Start();
        }

        internal bool IsTraceAllowed(ViewGenTraceLevel traceLevel)
        {
            return TraceLevel >= traceLevel;
        }

        internal override void ToCompactString(StringBuilder builder)
        {
            StringUtil.FormatStringBuilder(builder, "Trace Switch: {0}", m_traceLevel);
        }
    }
}
