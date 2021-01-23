using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;

using JesseRussell.LinqExtension;

using ParsingTools;

namespace ParserTranslation
{
    public class StringGrouper
    {
        #region public sub-classes
        public class ClassParameters
        {
            public ClassParameters(IEnumerable<(string opening, string closing)> brackets, IEnumerable<string> barriers, bool includeEmpty = false)
            {
                Brackets = Array.AsReadOnly(brackets == null ? new (string, string)[0] : brackets.ToArray());
                Barriers = Array.AsReadOnly(barriers == null ? new string[0] : barriers.ToArray());
                IncludeEmpty = includeEmpty;
            }
            public ReadOnlyCollection<(string opening, string closing)> Brackets { get; }
            public ReadOnlyCollection<string> Barriers { get; }
            public bool IncludeEmpty { get; } = true;
        }

        public class Group
        {
            #region Constructors
            internal Group(string text, string openingSequence, string closingSequence, WrappingType? wrapType)
            {
                Text = text;
                OpeningSequence = openingSequence;
                ClosingSequence = closingSequence;
                WrapType = wrapType;
            }
            public Group(string text, string openingSequence, string closingSequence)
            {
                Text = text;
                OpeningSequence = openingSequence;
                ClosingSequence = closingSequence;
                if (openingSequence == null || closingSequence == null) WrapType = null;
                else if (openingSequence == closingSequence)            WrapType = WrappingType.barriers;
                else                                                    WrapType = WrappingType.brackets;
            }
            public Group(string text) : this(text, null, null, null) { }
            #endregion


            #region Properties
            public string Text { get; }
            public string OpeningSequence { get; } = null;
            public string ClosingSequence { get; } = null;
            public WrappingType? WrapType { get; } = null;
            public bool IsWrapped => WrapType != null;
            public bool Empty => Text == null || Text == "";
            public bool EmptyUnwrapped => UnWrapped == null || UnWrapped == "";
            public string UnWrapped
            {
                get
                {
                    if (unWrapped != null) return unWrapped;
                    if (!IsWrapped) { unWrapped = Text; return Text; }
                    unWrapped = Text.Substring(OpeningSequence.Length, Text.Length - (OpeningSequence.Length + ClosingSequence.Length));
                    return unWrapped;
                }
            }
            private string unWrapped = null;
            #endregion

            public static implicit operator string(Group self) => self.Text;
            public override string ToString() => Text;
        }
        public enum WrappingType { brackets, barriers }
        public ClassParameters Parameters
        {
            get => parameters;
            set
            {
                parameters = value;

                finder = new SequenceFinder(value.Barriers.Concat(value.Brackets.Select(b => new string[] { b.opening, b.closing }).SelectMany(m => m)));

                barrierSet.Clear(); foreach (string b in value.Barriers) barrierSet.Add(b);
                openingMapToClosing = value.Brackets.ToDictionary(b => b.opening, b => b.closing);
                closingMapToOpening = value.Brackets.ToDictionary(b => b.closing, b => b.opening);
            }
        }
        public ClassParameters parameters;
        #endregion

        #region public Constructors
        public StringGrouper(ClassParameters parameters, IEnumerator<char> charStream)
        {
            if (parameters == null) throw new NullReferenceException();
            Parameters = parameters;
            this.charStream = charStream;
        }
        public StringGrouper(ClassParameters parameters, IEnumerable<char> characters) : this(parameters, characters.GetEnumerator()) { }
        #endregion

        public void Reset(IEnumerator<char> charStream = null, ClassParameters parameters = null)
        {
            charStream.Reset();
            finder.Reset();
            if (charStream != null) this.charStream = charStream;
            if (parameters != null) Parameters      = parameters;
        }
        public void Reset(IEnumerable<char> text = null, ClassParameters parameters = null) => Reset(text.GetEnumerator(), parameters);

        #region privates Fields
        private IEnumerator<char>          charStream;
        private Dictionary<string, int>    OpeningMapToDepth   = new Dictionary<string, int>();
        private Dictionary<string, bool>   barrierStatus         = new Dictionary<string, bool>();
        private StringBuilder              nextGroupText         = new StringBuilder();
        private SequenceFinder             finder;
        private bool                       active = true;
        private Dictionary<string, string> openingMapToClosing   = new Dictionary<string, string>();
        private Dictionary<string, string> closingMapToOpening   = new Dictionary<string, string>();
        private HashSet<string>            barrierSet            = new HashSet<string>();
        #endregion

        private void updateActive(bool bracketsFirst = true)
        {
            active = true;
            if (bracketsFirst)  foreach (int depth in OpeningMapToDepth.Select(p => p.Value)) if (depth > 0) { active = false; return; }
            foreach (bool status in barrierStatus.Select(p => p.Value)) if (status == true) { active = false; return; }
            if (!bracketsFirst) foreach (int depth in OpeningMapToDepth.Select(p => p.Value)) if (depth > 0) { active = false; return; }
        }

        private void addDepth(string openingBracket, int amount = 1)
        {
            OpeningMapToDepth.EnsureKey(openingBracket, 0);
            if ((OpeningMapToDepth[openingBracket] += amount) != 0)
                active = false;
            else
                if (active != true) updateActive();
        }
        private void subDepth(string openingBracket, int amount = 1) => addDepth(openingBracket, -amount);

        private void setBarrierStatus(string barrier, bool status)
        {
            barrierStatus.EnsureKey(barrier, false);
            if ((barrierStatus[barrier] = status) == true)
                active = false;
            else
                if (active != true) updateActive(false);
        }
        private void toggleBarrierStatus(string barrier)
        {
            barrierStatus.EnsureKey(barrier, false);
            if ((barrierStatus[barrier] ^= true) == true)
                active = false;
            else
                if (active != true) updateActive(false);
        }

        char? c = null;
        public Group NextGroup()
        {
            char prev;
            WrappingType wtype = default;

            if (charStream.MoveNext()) {
                if (c != null) finder.Look((char)c, charStream.Current);
                c = charStream.Current;
            }
            else
            {
                c = null;
                return null;
            }

            nextGroupText.Append(c);

            while (true)
            {
                bool initActive = active;


                prev = (char)c;
                c = charStream.MoveNext() ? charStream.Current : (char?)null;

                string sequenceFound = finder.Look(prev, c);


                if (sequenceFound != null)
                {
                    if (barrierSet.Contains(sequenceFound)) { toggleBarrierStatus(sequenceFound); wtype = WrappingType.barriers; }
                    else if (openingMapToClosing.ContainsKey(sequenceFound))                     { addDepth(sequenceFound); wtype = WrappingType.brackets; }
                    else if (closingMapToOpening.TryGetValue(sequenceFound, out string opening)) { subDepth(opening); wtype = WrappingType.brackets; }
                }


                if (initActive != active)
                {
                    if (active)
                    {
                        if (Parameters.IncludeEmpty || nextGroupText.Length != 0)
                        {
                            string closing = sequenceFound;
                            string opening = (wtype == WrappingType.barriers) ? closing : closingMapToOpening[closing];
                            Group g = new Group(nextGroupText.ToString(), opening, closing, wtype);
                            nextGroupText.Clear(); nextGroupText.Append(c);
                            return g;
                        }
                    }
                    else
                    {
                        if (Parameters.IncludeEmpty || nextGroupText.Length - sequenceFound.Length != 0)
                        {
                            nextGroupText.Remove(nextGroupText.Length - sequenceFound.Length, sequenceFound.Length);
                            Group g = new Group(nextGroupText.ToString());
                            nextGroupText.Clear(); nextGroupText.Append(sequenceFound + c);
                            return g;
                        }
                    }
                }


                if (c == null)
                {
                    Group g = new Group(nextGroupText.ToString());
                    nextGroupText.Clear();
                    return g;
                }
                else nextGroupText.Append((char)c);
            }
        }
    }

    // experimental:
    public static class sgutil
    {
        public static IEnumerable<StringGrouper.Group> StringGroup(this string s, IEnumerable<(string opening, string closing)> brackets, IEnumerable<string> barriers= null, bool includeEmpty = false)
        {
            StringGrouper sgr = new StringGrouper(
                new StringGrouper.ClassParameters(brackets, barriers, includeEmpty),
                s);

            List<string> result = new List<string>();

            StringGrouper.Group g;
            while ((g = sgr.NextGroup()) != null) yield return g;
        }
    }
}
