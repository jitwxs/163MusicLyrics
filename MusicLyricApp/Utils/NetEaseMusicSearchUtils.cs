using System;
using System.Collections.Generic;
using MusicLyricApp.Exception;

namespace MusicLyricApp.Utils
{
    public class NetEaseMusicSearchUtils
    {
        private static int[] bPa2x(string biy1x)
        {
            if (string.IsNullOrWhiteSpace(biy1x))
            {
                return new int[] { };
            }

            var bt2x = biy1x.Length / 2;
            var bn2x = 0;
            var sx7q = new int[bt2x];

            for (int i = 0; i < bt2x; i++)
            {
                var qg6a = Convert.ToInt32(biy1x[bn2x++].ToString(), 16) << 4;
                var pT6N = Convert.ToInt32(biy1x[bn2x++].ToString(), 16);
                sx7q[i] = Fn1x(qg6a + pT6N);
            }

            return sx7q;
        }

        private static int Fn1x(int hq4u)
        {
            if (hq4u < -128)
            {
                return Fn1x(128 - (-128 - hq4u));
            }
            else if (hq4u >= -128 && hq4u <= 127)
            {
                return hq4u;
            }
            else if (hq4u > 127)
            {
                return Fn1x(-129 + hq4u - 127);
            }
            else
            {
                throw new MusicLyricException();
            }
        }

        private static int[] bPb2x()
        {
            var QZ7S = "fuck~%23%24%25%5E%26*(458";
            var bPh2x = QZ7S.Length;
            var wZ8R = new List<int>();

            for (var i = 0; i < bPh2x; i++)
            {
                if (QZ7S[i] == '%')
                {
                    if (i + 2 < bPh2x)
                    {
                        var res = bPa2x(QZ7S[++i] + "" + QZ7S[++i])[0];
                        wZ8R.Add(res);
                    }
                    else
                    {
                        throw new MusicLyricException();
                    }
                }
                else
                {
                    wZ8R.Add(QZ7S[i]);
                }
            }

            return wZ8R.ToArray();
        }

        private static int[] csp7i(int[] sl7e)
        {
            var bir1x = 64;
            var bPr2x = new int[bir1x];

            if (sl7e.Length >= bir1x)
            {
                return csw7p(sl7e, 0, bir1x);
            }
            else
            {
                for (var i = 0; i < bir1x; i++)
                {
                    bPr2x[i] = sl7e[i % sl7e.Length];
                }
            }

            return bPr2x;
        }

        private static int[] csw7p(int[] dd2x, int bnu3x, int bt2x)
        {
            var dH3x = new int[bt2x];

            if (dd2x.Length == 0)
            {
                return dH3x;
            }

            for (var i = 0; i < bt2x; i++)
            {
                dH3x[i] = dd2x[bnu3x + i];
            }

            return dH3x;
        }

        private static int csW7P(int bmC3x, int Po6i)
        {
            var one = Fn1x(bmC3x);
            var two = Fn1x(Po6i);
            return Fn1x(one ^ two);
        }

        private static int[][] csj7c(int[] bip1x)
        {
            var Ph6b = 64;

            if (bip1x == null || bip1x.Length % Ph6b != 0)
            {
                throw new MusicLyricException();
            }

            var bn2x = 0;
            var csi7b = bip1x.Length / Ph6b;

            var boe3x = new int[csi7b][];

            for (var i = 0; i < csi7b; i++)
            {
                boe3x[i] = new int[Ph6b];

                for (var j = 0; j < Ph6b; j++)
                {
                    boe3x[i][j] = bip1x[bn2x++];
                }
            }

            return boe3x;
        }

        private static int[] bPv2x(int[] Pa6U, int[] sl7e)
        {
            var bPo2x = 4;
            var Ph6b = 64;

            sl7e = csp7i(sl7e);

            var bos4w = sl7e;
            var bot4x = csj7c(Pa6U);
            var crZ7S = bot4x.Length;
            var Sp7i = new int[Ph6b * crZ7S];

            for (var i = 0; i < crZ7S; i++)
            {
                var bow4A = bPt2x(bot4x[i]);
                bow4A = bPt2x(bow4A);
                var boy4C = bOW2x(bow4A, bos4w);
                var crY7R = cth7a(boy4C, ctc7V(bos4w));
                boy4C = bOW2x(crY7R, sl7e);
                bnA3x(boy4C, 0, Sp7i, i * Ph6b, Ph6b);
                bos4w = bot4x[i];
            }

            var bPx2x = new int[bPo2x];
            bnA3x(Sp7i, Sp7i.Length - bPo2x, bPx2x, 0, bPo2x);
            var bt2x = csB7u(bPx2x);
            if (bt2x > Sp7i.Length)
            {
                throw new MusicLyricException();
            }

            var sx7q = new int[bt2x];
            bnA3x(Sp7i, 0, sx7q, 0, bt2x);
            return sx7q;
        }

        private static int[] bOW2x(int[] Xp8h, int[] bmT3x)
        {
            if (Xp8h == null || bmT3x == null || Xp8h.Length != bmT3x.Length)
            {
                return Xp8h;
            }

            var sx7q = new int[Xp8h.Length];

            for (var i = 0L; i < Xp8h.Length; i++)
            {
                sx7q[i] = csW7P(Xp8h[i], bmT3x[i]);
            }

            return sx7q;
        }

        private static int[] bPt2x(int[] bon4r)
        {
            var bt2x = bon4r.Length;
            var bPu2x = new int[bt2x];

            for (var i = 0; i < bt2x; i++)
            {
                bPu2x[i] = csg7Z(bon4r[i]);
            }

            return bPu2x;
        }

        private static int csg7Z(int bPs2x)
        {
            int[] csr7k =
            {
                82, 9, 106, -43, 48, 54, -91, 56, -65, 64, -93, -98, -127, -13, -41, -5, 124, -29, 57, -126, -101, 47,
                -1, -121, 52, -114, 67, 68, -60, -34, -23, -53, 84, 123, -108, 50, -90, -62, 35, 61, -18, 76, -107, 11,
                66, -6, -61, 78, 8, 46, -95, 102, 40, -39, 36, -78, 118, 91, -94, 73, 109, -117, -47, 37, 114, -8, -10,
                100, -122, 104, -104, 22, -44, -92, 92, -52, 93, 101, -74, -110, 108, 112, 72, 80, -3, -19, -71, -38,
                94, 21, 70, 87, -89, -115, -99, -124, -112, -40, -85, 0, -116, -68, -45, 10, -9, -28, 88, 5, -72, -77,
                69, 6, -48, 44, 30, -113, -54, 63, 15, 2, -63, -81, -67, 3, 1, 19, -118, 107, 58, -111, 17, 65, 79, 103,
                -36, -22, -105, -14, -49, -50, -16, -76, -26, 115, -106, -84, 116, 34, -25, -83, 53, -123, -30, -7, 55,
                -24, 28, 117, -33, 110, 71, -15, 26, 113, 29, 41, -59, -119, 111, -73, 98, 14, -86, 24, -66, 27, -4, 86,
                62, 75, -58, -46, 121, 32, -102, -37, -64, -2, 120, -51, 90, -12, 31, -35, -88, 51, -120, 7, -57, 49,
                -79, 18, 16, 89, 39, -128, -20, 95, 96, 81, 127, -87, 25, -75, 74, 13, 45, -27, 122, -97, -109, -55,
                -100, -17, -96, -32, 59, 77, -82, 42, -11, -80, -56, -21, -69, 60, -125, 83, -103, 97, 23, 43, 4, 126,
                -70, 119, -42, 38, -31, 105, 20, 99, 85, 33, 12, 125
            };

            var qg6a = (int)((uint)bPs2x >> 4) & 15;
            var pT6N = bPs2x & 15;
            var bn2x = qg6a * 16 + pT6N;

            return csr7k[bn2x];
        }

        private static int[] ctc7V(int[] biA1x)
        {
            if (biA1x == null)
            {
                return biA1x;
            }

            var sx7q = new int[biA1x.Length];
            for (var i = 0; i < biA1x.Length; i++)
            {
                sx7q[i] = Fn1x(0 - biA1x[i]);
            }

            return sx7q;
        }

        private static int[] cth7a(int[] biz1x, int[] bmr3x)
        {
            if (biz1x == null)
            {
                return null;
            }

            if (bmr3x == null)
            {
                return biz1x;
            }

            var sx7q = new int[biz1x.Length];
            var ctf7Y = bmr3x.Length;
            for (var i = 0; i < biz1x.Length; i++)
            {
                sx7q[i] = Fn1x(biz1x[i] + bmr3x[i % ctf7Y]);
            }

            return sx7q;
        }

        private static void bnA3x(int[] dd2x, int bnu3x, int[] sO7H, int csv7o, int bt2x)
        {
            if (dd2x == null || dd2x.Length == 0)
            {
                return;
            }

            if (sO7H == null)
            {
                throw new MusicLyricException();
            }

            if (dd2x.Length < bt2x)
            {
                throw new MusicLyricException();
            }

            for (var i = 0; i < bt2x; i++)
            {
                sO7H[csv7o + i] = dd2x[bnu3x + i];
            }
        }

        private static int csB7u(int[] xN9E)
        {
            var X1x = 0;
            X1x += (xN9E[0] & 255) << 24;
            X1x += (xN9E[1] & 255) << 16;
            X1x += (xN9E[2] & 255) << 8;
            X1x += xN9E[3] & 255;
            return X1x;
        }

        private static string bOY2x(int[] wZ8R)
        {
            var bt2x = wZ8R.Length;
            if (wZ8R == null || bt2x < 0)
            {
                return "";
            }

            var Pm6g = new List<string>();
            for (var i = 0; i < bt2x; i++)
            {
                Pm6g.Add(csQ7J(wZ8R[i]));
            }

            return string.Join("", Pm6g.ToArray());
        }

        private static string csQ7J(int dq2x)
        {
            var bOX2x = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f' };
            var Pm6g = new List<char>();

            Pm6g.Add(bOX2x[(int)((uint)dq2x >> 4) & 15]);
            Pm6g.Add(bOX2x[dq2x & 15]);

            return new string(Pm6g.ToArray());
        }

        /// <summary>
        /// https://s3.music.126.net/web/s/core_b0e0d978ad73b5ce345b2e65b37c3dd6.js?b0e0d978ad73b5ce345b2e65b37c3dd6
        /// </summary>
        public static string Decode(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return "";
            }

            var Pa6U = bPa2x(content);

            var sl7e = bPb2x();

            var boN4R = bPv2x(Pa6U, sl7e);

            var Kh5m = bOY2x(boN4R);

            var An9e = new List<char>();
            var boR4V = Kh5m.Length / 2;
            var bn2x = 0;
            for (var i = 0; i < boR4V; i++)
            {
                An9e.Add('%');
                An9e.Add(Kh5m[bn2x++]);
                An9e.Add(Kh5m[bn2x++]);
            }

            return Uri.UnescapeDataString(string.Join("", An9e.ToArray()));
        }
    }
}