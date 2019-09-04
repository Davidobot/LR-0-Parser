using System;
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
        public byte LHS;
        public List<byte> RHS = new List<byte>();

        public ProductionRule(byte lhs, List<byte> rhs) {
            LHS = lhs; RHS = rhs;
        }

        // assuming everything is 2 characters
        // String in format LHS->alpha_._B_beta or LHS->._alpha_B_beta
        public List<String> GetItems() { 
            List<String> items = new List<string>();

            StringBuilder sb = new StringBuilder();
            sb.Append(LHS); sb.Append("->");
            for (int i = 0; i < RHS.Count; i++) {
                sb.Append(RHS[i]);
                if (i != RHS.Count - 1)
                    sb.Append("_");
            }
            String woDots = sb.ToString();

            for (int i = 0; i <= RHS.Count; i++) {
                items.Add(woDots.Insert(i < 2 ? (4 + i * 2) : (6 + (i - 1) * 3), (i == 0) ? "._" : "_."));
            }

            return items;
        }
        
        override
        public string ToString() {
            StringBuilder sb = new StringBuilder();
            sb.Append(LHS); sb.Append("->");
            for (int i = 0; i < RHS.Count; i++) {
                sb.Append(RHS[i]);
                if (i != RHS.Count - 1)
                    sb.Append("_");
            }
            return sb.ToString();
        }
    }

    class Token {
        public byte type;
        public string data;

        public Token(byte t, string d) {
            type = t; data = d;
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

        static bool IsTerminal(byte a) {
            return a >= (byte)T.plus && a <= (byte)T.epsilon;
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
                        if (g.LHS == B) {
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

        static int GetStateID(HashSet<string> I, List<HashSet<string>> C) {
            for (int i = 0; i < C.Count; i++)
                if (I.SetEquals(C[i]))
                    return i;
            return -1;
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

                    foreach (NT x in Enum.GetValues(typeof(NT))) { // loop over all non-terminals
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

        // Assuming that when a left-recursive rule is found, there are just 2 paths;
        // ie A -> Aa|b becomes A -> bA' and A' -> aA' | eps
        static List<ProductionRule> RemoveLeftRecursion(List<ProductionRule> G) {
            List<ProductionRule> GG = new List<ProductionRule>();

            for (int i = 0; i < G.Count; i++) {
                ProductionRule g = G[i];

                if (g.LHS == g.RHS[0]) {
                    // g2.LHS is b
                    ProductionRule g2 = G[i + 1];

                    List<byte> temp = new List<byte>(g2.RHS); temp.Add((byte)(g.LHS + 20)); // 20 to make sure there is no overlap between nonterminals
                    GG.Add(new ProductionRule(g.LHS, temp)); // A -> bA'

                    temp = new List<byte>(g.RHS); temp.RemoveAt(0); temp.Add((byte)(g.LHS + 20));
                    GG.Add(new ProductionRule((byte)(g.LHS + 20), temp));  // A' -> aA'

                    GG.Add(new ProductionRule((byte)(g.LHS + 20), new List<byte> { (byte)T.epsilon })); // A' -> eps
                    i++; continue;
                }

                GG.Add(g);
            }

            return GG;
        }

        // calculate FIRST for all grammar symbols
        static Dictionary<byte, HashSet<byte>> FIRST(List<ProductionRule> G) {
            Dictionary<byte, HashSet<byte>> first = new Dictionary<byte, HashSet<byte>>();

            HashSet<byte> grammarSymbols = new HashSet<byte>();
            foreach (T x in Enum.GetValues(typeof(T)))
                grammarSymbols.Add((byte)x);
            foreach (ProductionRule pr in G)
                grammarSymbols.Add(pr.LHS);

            foreach (byte X in grammarSymbols)
                first.Add(X, new HashSet<byte>());

            while (true) {
                bool added = false;

                // Loop over all grammar symbols X
                foreach (byte X in grammarSymbols) {
                    // FIRST of a terminal is itself in a singleton
                    if (IsTerminal(X))
                        added = first[X].Add(X) | added;

                    foreach (ProductionRule pr in G) {
                        if (pr.LHS == X) {
                            // loop through Ys in production X -> Y_1 Y_2 ... Y_k
                            foreach (byte Y in pr.RHS) {
                                // Add all elements of Y_i to FIRST(X)
                                foreach (byte a in first[Y])
                                    added = first[X].Add(a) | added;

                                // only continue onto Y_{i+1} if FIRST(Y_i) contains eps
                                if (!first[Y].Contains((byte)T.epsilon))
                                    break;
                            }
                        }
                    }

                    // if X -> eps is a production, add eps to FIRST(X)
                    foreach (ProductionRule pr in G)
                        if (pr.LHS == X && pr.RHS.Contains((byte)T.epsilon))
                            added = first[X].Add((byte)T.epsilon) | added;
                }

                if (!added)
                    break;
            }

            return first;
        }

        // calculate FIRST for a given terminal/non-terminal string
        static HashSet<byte> FIRST(List<byte> beta, Dictionary<byte, HashSet<byte>> first) {
            HashSet<byte> f = new HashSet<byte>();

            bool addEps = beta.Count > 0;
            foreach (byte Y in beta) {
                // Add all elements of Y_i to FIRST(X)
                foreach (byte a in first[Y])
                    if (a != (byte)T.epsilon)
                        f.Add(a);

                // only continue onto Y_{i+1} if FIRST(Y_i) contains eps
                if (!first[Y].Contains((byte)T.epsilon))
                    addEps = false;
                    break;
            }

            if (addEps)
                f.Add((byte)T.epsilon);

            return f;
        }

        // calculate FOLLOW for all non-terminals
        static Dictionary<byte, HashSet<byte>> FOLLOW(Dictionary<byte, HashSet<byte>> first, List<ProductionRule> G) {
            Dictionary<byte, HashSet<byte>> follow = new Dictionary<byte, HashSet<byte>>();
            foreach (ProductionRule pr in G)
                if (!follow.ContainsKey(pr.LHS))
                    follow.Add(pr.LHS, new HashSet<byte>());

            // assume first production rule is the start symbol; insert $ into FOLLOW(start)
            follow[G[0].LHS].Add((byte)T.dollar);

            while (true) {
                bool added = false;

                foreach (ProductionRule pr in G) {
                    byte B;
                    // loop through all possibilities of A -> aBb (so that b is not null)
                    for (int i = 0; i < pr.RHS.Count - 1; i++) {
                        List<byte> a = pr.RHS.Take(i).ToList();
                        B = pr.RHS[i];
                        List<byte> b = pr.RHS.Skip(i + 1).ToList();

                        if (IsTerminal(B)) // B is NT
                            continue;
                        
                        // everything in FIRST(b), except eps, is in FOLLOW(B)
                        HashSet<byte> first_b = FIRST(b, first);
                        foreach (byte t in first_b)
                            if (t != (byte)T.epsilon)
                                added = follow[B].Add(t) | added;

                        // if FIRST(b) contains eps, then everything in FOLLOW(A) is in FOLLOW(B)
                        if (first_b.Contains((byte)T.epsilon))
                            foreach (byte t in follow[pr.LHS])
                                added = follow[B].Add(t) | added;
                    }

                    // if production A -> aB, then everything in FOLLOW(A) is in FOLLOW(B)
                    B = pr.RHS[pr.RHS.Count - 1];

                    // make sure B is a non-terminal
                    if (!IsTerminal(B))
                        foreach (byte t in follow[pr.LHS])
                            added = follow[B].Add(t) | added;
                }

                if (!added)
                    break;
            }

            return follow;
        }

        // dot string to production rule number
        static int LookupProductionNum(string a, List<ProductionRule> G) {
            byte LHS = Byte.Parse(a.Substring(0, 2));
            string[] s = a.Split('>')[1].Replace(".", "").Replace("__", "_").Split('_');
            List<byte> gs = new List<byte>();
            foreach (string t in s)
                if (!t.Equals(""))
                    gs.Add(Byte.Parse(t));

            for (int i = 0; i < G.Count; i++) {
                if (G[i].RHS.SequenceEqual(gs) && LHS == G[i].LHS)
                    return i;
            }

            return -1;
        }

        // Parse a given input, returns true if successful, false otherwise
        // input finishes with $
        static bool Parse(List<Token> input, string[,] Action, byte[,] Goto, List<ProductionRule> G) {
            Stack<byte> stack = new Stack<byte>(); stack.Push(0); // initial state
            int a_i = 0; // index into input

            Token a = input[a_i];
            while (true) {
                byte s = stack.Peek();

                if (Action[s, a.type - (byte)T.plus].StartsWith("s_")) { // shift
                    foreach (var it in stack.Reverse())
                        Console.Write(it + " ");

                    stack.Push(Byte.Parse(Action[s, a.type - (byte)T.plus].Split('_')[1]));
                    a = input[++a_i];

                    Console.WriteLine("Shift");
                } else if (Action[s, a.type - (byte)T.plus].StartsWith("r_")) { // reduce
                    foreach (var it in stack.Reverse())
                        Console.Write(it + " ");

                    int prod_num = int.Parse(Action[s, a.type - (byte)T.plus].Split('_')[1]);
                    for (int i = 0; i < G[prod_num].RHS.Count; i++)
                        stack.Pop();

                    byte t = stack.Peek();
                    stack.Push(Goto[t, G[prod_num].LHS - (byte)NT.EPDash]);
                    Console.WriteLine(G[prod_num]);
                } else if (Action[s, a.type - (byte)T.plus].Equals("acc")) {
                    foreach (var it in stack.Reverse())
                        Console.Write(it + " ");
                    Console.WriteLine("Accepted");
                    return true;
                } else {
                    Console.WriteLine("Rejected");
                    return false;
                }
            }
        }

        // Define Grammar in terms of production rules
        static List<ProductionRule> grammar = new List<ProductionRule> {
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
            List<ProductionRule> GRAMMAR = textbookGrammar;

            // Constructing a SLR-parsing table
            // Step 1, construct C,  the collection of sets of LR(0) items for G'.
            List<HashSet<string>> C = ITEMS(GRAMMAR);

            // Step 2, construct Action and Goto tables
            //      ACTION table: [number of states, number of terminals]
            //      for indexing, terminal value = terminal value - 10 (T.plus)
            string[,] Action = new string[C.Count, T.epsilon - T.plus + 1];
            for (int i = 0; i < C.Count; i++)
                for (int j = 0; j < T.epsilon - T.plus + 1; j++)
                    Action[i, j] = "";
            // actions:
            //          s_i means shift and stack state i
            //          r_j means reduce by the production numbered j
            //          acc means accept
            //          otherwise (blank) means error

            //      GOTO table: [number of states, number of non-terminals]
            //      for indexing, nonterminal value = nonterminal value - 20 (NT.EPDash)
            byte[,] Goto = new byte[C.Count, NT.S - NT.EPDash + 1];

            for (int i = 0; i < C.Count; i++) {
                foreach (NT A in Enum.GetValues(typeof(NT))) {
                    byte a = (byte)A;
                    HashSet<string> temp = GOTO(C[i].ToList(), a, GRAMMAR);
                    int j = GetStateID(temp, C);
                    if (j != -1)
                        Goto[i, (int)(a - NT.EPDash)] = (byte) j;
                }
            }

            // To calculate Action, need to calculate FOLLOW, which needs FIRST, which needs non-left-recursive grammar
            List<ProductionRule> NLRGrammar = RemoveLeftRecursion(GRAMMAR); // remove augmented grammar rule .Skip(1).ToList()
            Dictionary<byte, HashSet<byte>> first = FIRST(NLRGrammar);
            Dictionary<byte, HashSet<byte>> follow = FOLLOW(first, GRAMMAR);

            for (int i = 0; i < C.Count; i++) {
                foreach (string t in C[i]) {
                    byte a = ExtractNextToDot(t);

                    if (IsTerminal(a)) {
                        int j = GetStateID(GOTO(C[i].ToList(), a, GRAMMAR), C);
                        if (j != -1)
                            Action[i, a - (byte)T.plus] = "s_" + j;
                    } else if (a == 0) { // dot at the end of the string
                        // end of string and it's the starting symbol
                        if (t.Substring(0, 2).Equals(GRAMMAR[0].LHS.ToString()))
                            Action[i, (byte)T.dollar - (byte)T.plus] = "acc";
                        else { // just A -> a.
                            byte A = Byte.Parse(t.Substring(0, 2));
                            HashSet<byte> temp = follow[A];
                            foreach (byte aa in temp)
                                Action[i, aa - (byte)T.plus] = "r_" + LookupProductionNum(t, GRAMMAR);
                        }
                    }
                }
            }

            Console.WriteLine("== GOTO ==");
            for (int i = -1; i < C.Count; i++) {
                Console.Write("\n{0}\t|\t", i);
                foreach (T x in Enum.GetValues(typeof(NT))) {
                    byte y = (byte)x;

                    if (i == -1)
                        Console.Write("{0}\t", y);
                    else
                        Console.Write("{0}\t", Goto[i, y - (byte)NT.EPDash]);
                }
            }

            Console.WriteLine("\n\n== ACTION ==");
            for (int i = -1; i < C.Count; i++) {
                Console.Write("\n{0}\t|\t", i);
                foreach (T x in Enum.GetValues(typeof(T))) {
                    byte y = (byte)x;

                    if (i == -1)
                        Console.Write("{0}\t", y);
                    else
                        Console.Write("{0}\t", Action[i, y - (byte)T.plus]);
                }
            }

            Console.WriteLine("\n\n== PARSE ==");
            Parse(new List<Token> { new Token((byte)T.id, ""), new Token((byte)T.times, ""), new Token((byte)T.id, ""), new Token((byte)T.plus, ""), new Token((byte)T.id, ""), new Token((byte)T.dollar, ""), }, Action, Goto, GRAMMAR);
            Console.WriteLine("\n\n== PARSE ==");
            Parse(new List<Token> { new Token((byte)T.id, ""), new Token((byte)T.times, ""), new Token((byte)T.id, ""), new Token((byte)T.plus, ""), new Token((byte)T.dollar, ""), }, Action, Goto, GRAMMAR);

            Console.ReadLine();
        }
    }
}
