using System.Drawing;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Drawing.Imaging;

namespace Signapse.Web
{
    public class IconBuilder
    {
        public byte[] LogoImageData(string email)
        {
            using var bmp = GenerateGravatar(email, 256);
            using var ms = new MemoryStream();

            bmp.Save(ms, ImageFormat.Png);
            
            return ms.ToArray();
        }

        readonly static Color[] ColorTints = {
            Color.AliceBlue,
            Color.OrangeRed,
            Color.RebeccaPurple,
            Color.LightGreen,
            Color.YellowGreen,
            Color.BlueViolet,
            Color.LightBlue,
            Color.Coral
        };
        static Bitmap GenerateGravatar(string email, int size)
        {
            // Convert the email address to its lowercase form and trim any leading or trailing whitespace.
            string emailHash = ComputeEmailHash(email);

            // Use the hexadecimal string to seed a pseudorandom number generator.
            Random random = new Random(emailHash.GetHashCode());

            // Generate a Perlin noise pattern.
            Bitmap bitmap = new Bitmap(size, size);
            PerlinNoise noise = new PerlinNoise(random.Next());
            Color colorTint = ColorTints[random.Next(ColorTints.Length)];
            double scale = 0.05;
            double z = random.NextDouble() * 100;
            for (int y = 0; y < size; y++)
            {
                int yFactor = y - (y % (size / 8));
                for (int x = 0; x < size; x++)
                {
                    int xFactor = x - (x % (size / 8));
                    double value = noise.Noise(xFactor * scale, yFactor * scale, z);
                    int colorValue = (int)(value * 128 + 128);
                    Color color = Color.FromArgb(colorValue, colorValue, colorValue);

                    color = Color.FromArgb(
                        (int)(color.R * colorTint.R / 255f),
                        (int)(color.G * colorTint.G / 255f),
                        (int)(color.B * colorTint.B / 255f)
                    );

                    bitmap.SetPixel(x, y, color);
                }
            }

            // Mirror the pattern horizontally
            for (int y = 0; y < size; y++)
            {
                for (int x = size / 2; x < size; x++)
                {
                    // Get the color of the corresponding pixel in the first half of the image
                    Color color = bitmap.GetPixel(size - x - 1, y);

                    // Set the color of the current pixel
                    bitmap.SetPixel(x, y, color);
                }
            }

            // Mirror the pattern vertically
            for (int y = size / 2; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Get the color of the corresponding pixel in the first half of the image
                    Color color = bitmap.GetPixel(x, size - y - 1);

                    // Set the color of the current pixel
                    bitmap.SetPixel(x, y, color);
                }
            }

            return bitmap;
        }

        static void SaveImage(Bitmap image, string fileName, ImageFormat format)
        {
            // Step 5: Display the image to the user or save it to a file.
            using (FileStream stream = new FileStream(fileName, FileMode.Create))
            {
                image.Save(stream, format);
            }
        }

        static string ComputeEmailHash(string email)
        {
            // Step 1: Convert the email address to its lowercase form and trim any leading or trailing whitespace.
            email = email.ToLower().Trim();

            // Step 2: Generate an MD5 hash of the lowercase email address.
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.ASCII.GetBytes(email);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Step 3: Convert the MD5 hash to a hexadecimal string.
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }
    }

    class PerlinNoise
    {
        private int[] permutation;
        private double[] gradientsX;
        private double[] gradientsY;
        private double[] gradientsZ;

        public PerlinNoise(int seed)
        {
            Random random = new Random(seed);
            permutation = new int[256 * 2];
            gradientsX = new double[256 * 2];
            gradientsY = new double[256 * 2];
            gradientsZ = new double[256 * 2];

            for (int i = 0; i < 256; i++)
            {
                permutation[i] = i;
                gradientsX[i] = random.NextDouble() * 2 - 1;
                gradientsY[i] = random.NextDouble() * 2 - 1;
                gradientsZ[i] = random.NextDouble() * 2 - 1;
            }

            for (int i = 0; i < 256; i++)
            {
                int j = random.Next(256 - i) + i;
                int temp = permutation[i];
                permutation[i] = permutation[j];
                permutation[j] = temp;
            }

            for (int i = 0; i < 256; i++)
            {
                permutation[i + 256] = permutation[i];
                gradientsX[i + 256] = gradientsX[i];
                gradientsY[i + 256] = gradientsY[i];
                gradientsZ[i + 256] = gradientsZ[i];
            }
        }

        private double Fade(double t)
        {
            return t * t * t * (t * (t * 6 - 15) + 10);
        }

        private double Lerp(double t, double a, double b)
        {
            return a + t * (b - a);
        }

        private double Grad(int hash, double x, double y, double z)
        {
            int h = hash & 15;
            double u = h < 8 ? x : y;
            double v = h < 4 ? y : (h == 12 || h == 14 ? x : z);
            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
        }

        public double Noise(double x, double y, double z)
        {
            int X = (int)Math.Floor(x) & 255;
            int Y = (int)Math.Floor(y) & 255;
            int Z = (int)Math.Floor(z) & 255;

            x -= Math.Floor(x);
            y -= Math.Floor(y);
            z -= Math.Floor(z);

            double u = Fade(x);
            double v = Fade(y);
            double w = Fade(z);

            int A = permutation[X] + Y;
            int AA = permutation[A] + Z;
            int AB = permutation[A + 1] + Z;
            int B = permutation[X + 1] + Y;
            int BA = permutation[B] + Z;
            int BB = permutation[B + 1] + Z;

            double gradAAx = Grad(permutation[AA], x, y, z);
            double gradAAy = Grad(permutation[AA + 1], x, y, z - 1);
            double gradABx = Grad(permutation[AB], x, y - 1, z);
            double gradABy = Grad(permutation[AB + 1], x, y - 1, z - 1);
            double gradBAx = Grad(permutation[BA], x - 1, y, z);
            double gradBAy = Grad(permutation[BA + 1], x - 1, y, z - 1);
            double gradBBx = Grad(permutation[BB], x - 1, y - 1, z);
            double gradBBy = Grad(permutation[BB + 1], x - 1, y - 1, z - 1); double lerpAA = Lerp(u, gradAAx, gradABx);
            double lerpAB = Lerp(u, gradAAy, gradABy);
            double lerpBA = Lerp(u, gradBAx, gradBBx);
            double lerpBB = Lerp(u, gradBAy, gradBBy);

            double lerpAAAB = Lerp(v, lerpAA, lerpAB);
            double lerpBAAB = Lerp(v, lerpBA, lerpBB);

            double result = Lerp(w, lerpAAAB, lerpBAAB);

            // Normalize the result between 0 and 1
            return Math.Abs((result + 1) * NormalizationFactor);
        }
        
        readonly static double NormalizationFactor = 1 / (2 * Math.Sqrt(3));
    }
}
