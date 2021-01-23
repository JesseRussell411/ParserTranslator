using System.Diagnostics;
using System.Text;
using System.Linq;
using System;

using ParserTranslation;
using ParsingTools;
using JesseRussell.LinqExtension;

namespace debugging
{
    class Debugging
    {
        static void Main(string[] args)
        {
            string text = "\"123\"123";
            //StringGrouper grouper = new StringGrouper(
            //    new StringGrouper.ClassParameters(new (string, string)[] { ("(", ")"),("[", "]")}, "\"".Enumerate(), true),
            //    text);

            //StringGrouper.Group g;
            //while ((g = grouper.NextGroup()) != null)
            //    Console.WriteLine(g.Empty ? "empty group" : g);

            Console.WriteLine(text.StringGroup(null, "\"".Enumerate()).Select(g => g.UnWrapped).Aggregate((t, c) => $"{t}\n{c}"));
            //string text = " var += b == 1 + 8 + 7 === \"7\" ";
            //string[] tokens = new string[] { "+=", "==", "+", "+", "===" };

            //Stopwatch sw = new Stopwatch();
            //sw.Start();

            //TokenFinder finder = new TokenFinder(tokens);

            //char c;
            //char? next;
            //int text_lastIndex = text.Length - 1;
            //string token;
            //for(int i = 0; i < text.Length; i++)
            //{
            //    c = text[i];
            //    next = i == text_lastIndex ? null : (char?) text[i + 1];

            //    token = finder.Look(c, next);


            //    Console.Write(c + " ");
            //    Console.WriteLine(token ?? "");
            //}

            //sw.Stop();
            //Console.WriteLine(sw.ElapsedMilliseconds);

        }
    }
}
