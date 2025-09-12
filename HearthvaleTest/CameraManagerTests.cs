using Hearthvale.GameCode.Managers;
using Hearthvale.GameCode.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Xunit;

namespace HearthvaleTest
{
    public class CameraManagerTests
    {
        private GraphicsDevice CreateGraphicsDevice()
        {
            return new GraphicsDevice(GraphicsAdapter.DefaultAdapter, GraphicsProfile.Reach, new PresentationParameters());
        }

        private Camera2D CreateCamera(GraphicsDevice gd)
        {
            return new Camera2D(gd.Viewport);
        }

        [Fact]
        public void Update_WithSmoothing_MovesTowardTargetButDoesNotSnap()
        {
            var gd = CreateGraphicsDevice();
            var cam = CreateCamera(gd);
            CameraManager.Initialize(cam);
            cam.Position = Vector2.Zero;

            var target = new Vector2(100, 100);
            // Use default smoothing (~0.18) via null
            int tileSize = 32;
            int tiles = 2000 / tileSize;
            CameraManager.Instance.Update(target, tiles, tiles, tileSize, new GameTime(), null);

            Assert.NotEqual(target, cam.Position); // should not snap
            Assert.True(cam.Position.X > 0 && cam.Position.Y > 0); // moved toward target
        }

        [Fact]
        public void Update_WithSnap_ReachesTargetOrClamp()
        {
            var gd = CreateGraphicsDevice();
            var cam = CreateCamera(gd);
            CameraManager.Initialize(cam);
            // Camera starts at (0,0); after snap we expect clamped to map
            var target = new Vector2(150, 220);
            int tileSize = 32; int tiles = 2000 / tileSize;
            CameraManager.Instance.Update(target, tiles, tiles, tileSize, new GameTime(), 1f);
            // Compute expected clamped values given viewport half extents
            float halfW = gd.Viewport.Width / 2f;
            float halfH = gd.Viewport.Height / 2f;
            float mapWidthPx = tiles * tileSize;
            float mapHeightPx = tiles * tileSize;
            float minX = halfW;
            float maxX = mapWidthPx - halfW;
            if (maxX < minX) minX = maxX = mapWidthPx / 2f;
            float minY = halfH;
            float maxY = mapHeightPx - halfH;
            if (maxY < minY) minY = maxY = mapHeightPx / 2f;
            float expectedX = MathHelper.Clamp(target.X, minX, maxX);
            float expectedY = MathHelper.Clamp(target.Y, minY, maxY);
            Assert.Equal(new Vector2(expectedX, expectedY), cam.Position);
        }

        [Fact]
        public void Update_ClampSmallMap_CentersCamera()
        {
            var gd = CreateGraphicsDevice();
            var cam = CreateCamera(gd);
            CameraManager.Initialize(cam);
            // Large target outside tiny map
            var target = new Vector2(500, 500);
            int mapWidthPixels = 200;
            int mapHeightPixels = 150;
            int tileSize = 32;
            int tilesX = mapWidthPixels / tileSize;
            int tilesY = mapHeightPixels / tileSize;
            CameraManager.Instance.Update(target, tilesX, tilesY, tileSize, new GameTime(), 1f);
            // Map smaller than viewport: clamp logic collapses to using computed tile pixel size (tiles * tileSize)
            float expectedX = (tilesX * tileSize) / 2f; // 6*32=192 -> /2 = 96
            float expectedY = (tilesY * tileSize) / 2f; // 4*32=128 -> /2 = 64
            Assert.InRange(cam.Position.X, expectedX - 0.1f, expectedX + 0.1f);
            Assert.InRange(cam.Position.Y, expectedY - 0.1f, expectedY + 0.1f);
        }
    }
}
