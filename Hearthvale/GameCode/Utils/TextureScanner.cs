using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Hearthvale.GameCode.Utils
{
    public static class TextureScanner
    {
        public static List<Rectangle> ScanForObjects(Texture2D texture)
        {
            var regions = new List<Rectangle>();
            if (texture == null) return regions;

            Color[] data = new Color[texture.Width * texture.Height];
            texture.GetData(data);

            bool[,] visited = new bool[texture.Width, texture.Height];

            for (int y = 0; y < texture.Height; y++)
            {
                for (int x = 0; x < texture.Width; x++)
                {
                    if (!visited[x, y] && data[y * texture.Width + x].A > 0)
                    {
                        // Found a new object, flood fill to find its bounds
                        Rectangle bounds = FloodFill(data, visited, texture.Width, texture.Height, x, y);
                        regions.Add(bounds);
                    }
                }
            }

            return regions;
        }

        private static Rectangle FloodFill(Color[] data, bool[,] visited, int width, int height, int startX, int startY)
        {
            int minX = startX;
            int maxX = startX;
            int minY = startY;
            int maxY = startY;

            Queue<Point> queue = new Queue<Point>();
            queue.Enqueue(new Point(startX, startY));
            visited[startX, startY] = true;

            while (queue.Count > 0)
            {
                Point p = queue.Dequeue();

                if (p.X < minX) minX = p.X;
                if (p.X > maxX) maxX = p.X;
                if (p.Y < minY) minY = p.Y;
                if (p.Y > maxY) maxY = p.Y;

                // Check neighbors
                CheckNeighbor(data, visited, width, height, p.X + 1, p.Y, queue);
                CheckNeighbor(data, visited, width, height, p.X - 1, p.Y, queue);
                CheckNeighbor(data, visited, width, height, p.X, p.Y + 1, queue);
                CheckNeighbor(data, visited, width, height, p.X, p.Y - 1, queue);
                
                // Diagonals for better connectivity
                CheckNeighbor(data, visited, width, height, p.X + 1, p.Y + 1, queue);
                CheckNeighbor(data, visited, width, height, p.X - 1, p.Y - 1, queue);
                CheckNeighbor(data, visited, width, height, p.X + 1, p.Y - 1, queue);
                CheckNeighbor(data, visited, width, height, p.X - 1, p.Y + 1, queue);
            }

            return new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        private static void CheckNeighbor(Color[] data, bool[,] visited, int width, int height, int x, int y, Queue<Point> queue)
        {
            if (x >= 0 && x < width && y >= 0 && y < height)
            {
                if (!visited[x, y] && data[y * width + x].A > 0)
                {
                    visited[x, y] = true;
                    queue.Enqueue(new Point(x, y));
                }
            }
        }
    }
}
