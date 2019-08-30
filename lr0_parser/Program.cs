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
        plus = 10, minus = 11, times = 12, cos = 13, factorial = 14, bracket_open = 15, bracket_close = 16, id = 17, dollar = 18
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
        public List<String> GetItems() { // String in format LHS->alpha_._B_beta or LHS->._alpha_B_beta
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
        // 
        static byte ExtractNextToDot(String item) {

        }

        // Calculate CLOSURE for a given grammar
        static HashSet<string> CLOSURE(List<ProductionRule> G) {
            HashSet<string> close = new HashSet<string>();

            // add every item, derived from grammar into set
            foreach (ProductionRule g in G)
                foreach (string it in g.GetItems())
                    close.Add(it);

            // if A -> a.Bb is in set and B->y is a production, then add B->.y into set
            while (true) {
                bool added = false;

                foreach (string A in close) {

                }

                if (!added)
                    break;
            }

            return close;
        }

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

        static void Main(string[] args) {
        }
    }
}
