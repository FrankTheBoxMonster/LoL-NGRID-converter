using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoLNGRIDConverter {
    public struct Color {
        public int r;
        public int g;
        public int b;

        public Color(int r, int g, int b) {
            this.r = r;
            this.g = g;
            this.b = b;
        }

        public static Color operator*(Color color, float value) {
            int r = (int) (color.r * value);
            int g = (int) (color.g * value);
            int b = (int) (color.b * value);

            return new Color(r, g, b);
        }


        public static bool operator==(Color lhs, Color rhs) {
            return lhs.Equals(rhs);
        }

        public static bool operator!=(Color lhs, Color rhs) {
            return (lhs.Equals(rhs) == false);
        }

        public bool Equals(Color other) {
            return (this.r == other.r && this.g == other.g && this.b == other.b);
        }
    }
}
