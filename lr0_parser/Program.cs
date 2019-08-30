﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lr0_parser {
    /*
    *  Terminals: +, -, *, cos, !, (, ), id 
    *      id is signedfloat
    *      
    *  Non-terminals: expression (EP', EP), expr (EM), term (TM),
    *                  trig (TR), factorial (F), statement (S)
    *      
    *  Grammar:
    *      EP' -> EP
    *      EP  -> EP + EM | EM
    *      EM  -> EM - TM | TM
    *      TM  -> TM * TR | TR
    *      TR  -> cos F   | F
    *      F   -> S !     | S
    *      S   -> ( EP )  | id
    *      
    */

    // Terminals
    enum T {
        plus = 10, minus = 11, times = 12, cos = 13, factorial = 14, bracket_open = 15, bracket_close = 16, id = 17, dollar = 18, epsilon = 19
    }

    // Non-terminals
    enum NT {
        EPDash = 20, EP = 21, EM = 22, TM = 23, TR = 24, F = 25, S = 26
    }

    class ProductionRule {
        public byte RHS;
        public List<byte> LHS = new List<byte>();

        public ProductionRule(byte rhs, List<byte> lhs) {
            RHS = rhs;
            LHS = lhs;
        }

        // assuming everything is 2 characters
        // String in format LHS->alpha_._B_beta or LHS->._alpha_B_beta
        public List<String> GetItems() { 
            List<String> items = new List<string>();

            StringBuilder sb = new StringBuilder();
            sb.Append(RHS); sb.Append("->");
            for (int i = 0; i < LHS.Count; i++) {
                sb.Append(LHS[i]);
                if (i != LHS.Count - 1)
                    sb.Append("_");
            }
            String woDots = sb.ToString();

            for (int i = 0; i <= LHS.Count; i++) {
                items.Add(woDots.Insert(i < 2 ? (4 + i * 2) : (6 + (i - 1) * 3), (i == 0) ? "._" : "_."));
            }

            return items;
        }
    }

    class Program {
        // extracts terminal/non-terminal from directly to the right of the dot in an item
        static byte ExtractNextToDot(string item) {
            item = item.Split('.')[1]; // one dot, take RHS
            item = item.Replace("_", "");

            if (item.CompareTo("") == 0)
                return 0; // dot is at end of item

            return Byte.Parse(item.Substring(0, 2));
        }

        // moves dot one over
        static string MoveDotToRight(string item) {
            int dotPos = item.IndexOf('.') + 3; // move it over, accounting for _ and removed .
            item = item.Replace(".", "");
            item = item.Insert(dotPos, "_."); item = item.Replace(">_", ">"); item = item.Replace("__", "_");

            return item;
        }

        // Calculate CLOSURE for a given set of items, given a grammar
        static HashSet<string> CLOSURE(List<string> I, List<ProductionRule> G) {
            HashSet<string> close = new HashSet<string>();

            // add every item in I to set [rule 1]
            foreach (string it in I)
                close.Add(it);

            // if A -> a.Bb is in set and B->y is a production, then add B->.y into set
            while (true) {
                List<string> toAdd = new List<string>();

                foreach (string A in close) {
                    byte B = ExtractNextToDot(A);

                    foreach (ProductionRule g in G)
                        if (g.RHS == B) {
                            string y = g.GetItems()[0];

                            if (!close.Contains(y)) {
                                toAdd.Add(y); // index 0 is the one with the dot in first position
                            }
                        }
                }

                foreach (string y in toAdd)
                    close.Add(y);

                if (toAdd.Count == 0)
                    break;
            }

            return close;
        }

        // Calculate the set GOTO(I, op) given an item set I and operator op; as well as grammar
        static HashSet<string> GOTO(List<string> I, byte op, List<ProductionRule> G) {
            HashSet<string> gotoo = new HashSet<string>();

            foreach (string it in I)
                if (ExtractNextToDot(it) == op) // check that op is immediately to the right
                    foreach (string itc in CLOSURE(new List<string> { MoveDotToRight(it) }, G)) // calculate closure on new item
                        gotoo.Add(itc);

            return gotoo;
        }

        static bool Contains(List<HashSet<string>> C, HashSet<string> vs) {
            foreach(HashSet<string> c in C) {
                if (c.SetEquals(vs))
                    return true;
            }
            return false;
        }

        // Caluclate C, the canonical collection of sets of LR(0) items for augmented grammar G
        // This is the collections I_i, as shown in figure 4.31
        static List<HashSet<string>> ITEMS(List<ProductionRule> G) {
            List<HashSet<string>> C = new List<HashSet<string>>();
            C.Add(CLOSURE(new List<string> { G[0].GetItems()[0] }, G)); // assume entry point S' is first item of grammar production rules

            while (true) {
                List<HashSet<string>> toAdd = new List<HashSet<string>>();

                foreach (HashSet<string> I in C) {
                    foreach (T x in Enum.GetValues(typeof(T))) { // loop over all terminals
                        byte X = (byte)x;
                        HashSet<string> gotoo = GOTO(I.ToList(), X, G);

                        if (gotoo.Count > 0 && !Contains(C, gotoo))
                            toAdd.Add(gotoo);
                    }

                    foreach (T x in Enum.GetValues(typeof(NT))) { // loop over all non-terminals
                        byte X = (byte)x;
                        HashSet<string> gotoo = GOTO(I.ToList(), X, G);

                        if (gotoo.Count > 0 && !Contains(C, gotoo))
                            toAdd.Add(gotoo);
                    }
                }

                foreach (HashSet<string> hs in toAdd)
                    C.Add(hs);

                if (toAdd.Count == 0)
                    break;
            }

            return C;
        }

        // Define Grammar in terms of production rules
        List<ProductionRule> grammar = new List<ProductionRule> {
            new ProductionRule((byte) NT.EPDash, new List<byte>{ (byte) NT.EP }),
            new ProductionRule((byte) NT.EP, new List<byte>{ (byte) NT.EP, (byte) T.plus, (byte) NT.EM }),
            new ProductionRule((byte) NT.EP, new List<byte>{ (byte) NT.EM }),
            new ProductionRule((byte) NT.EM, new List<byte>{ (byte) NT.EM, (byte) T.minus, (byte) NT.TM }),
            new ProductionRule((byte) NT.EM, new List<byte>{ (byte) NT.TM }),
            new ProductionRule((byte) NT.TM, new List<byte>{ (byte) NT.TM, (byte) T.times, (byte) NT.TR }),
            new ProductionRule((byte) NT.TM, new List<byte>{ (byte) NT.TR }),
            new ProductionRule((byte) NT.TR, new List<byte>{ (byte) T.cos, (byte) NT.F }),
            new ProductionRule((byte) NT.TR, new List<byte>{ (byte) NT.F }),
            new ProductionRule((byte) NT.F, new List<byte>{ (byte) NT.S, (byte) T.factorial }),
            new ProductionRule((byte) NT.F, new List<byte>{ (byte) NT.S }),
            new ProductionRule((byte) NT.S, new List<byte>{ (byte) T.bracket_open, (byte) NT.EP, (byte) T.bracket_close }),
            new ProductionRule((byte) NT.S, new List<byte>{ (byte) T.id }),
        };

        static List<ProductionRule> textbookGrammar = new List<ProductionRule> {
            new ProductionRule((byte) NT.EPDash, new List<byte>{ (byte) NT.EP }),
            new ProductionRule((byte) NT.EP, new List<byte>{ (byte) NT.EP, (byte) T.plus, (byte) NT.TM }),
            new ProductionRule((byte) NT.EP, new List<byte>{ (byte) NT.TM }),
            new ProductionRule((byte) NT.TM, new List<byte>{ (byte) NT.TM, (byte) T.times, (byte) NT.S }),
            new ProductionRule((byte) NT.TM, new List<byte>{ (byte) NT.S }),
            new ProductionRule((byte) NT.S, new List<byte>{ (byte) T.bracket_open, (byte) NT.EP, (byte) T.bracket_close }),
            new ProductionRule((byte) NT.S, new List<byte>{ (byte) T.id }),
        };

        static void Main(string[] args) {
            List<HashSet<string>> vs = ITEMS(textbookGrammar);

            Console.ReadLine();
        }
    }
}
