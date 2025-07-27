using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

public class CombatEffectsManager
{
    private readonly Hearthvale.UI.Camera2D _camera;
    private readonly List<DamageNumber> _damageNumbers = new();

    public CombatEffectsManager(Hearthvale.UI.Camera2D camera)
    {
        _camera = camera;
    }

    public void PlayHitEffects(Vector2 position, int damage)
    {
        // Screen shake
        _camera.Shake(0.08f, 1.5f);

        // Damage number
        _damageNumbers.Add(new DamageNumber(position, damage));
    }

    public void Update(GameTime gameTime)
    {
        for (int i = _damageNumbers.Count - 1; i >= 0; i--)
        {
            _damageNumbers[i].Update(gameTime);
            if (_damageNumbers[i].IsExpired)
                _damageNumbers.RemoveAt(i);
        }
    }

    public void Draw(SpriteBatch spriteBatch, SpriteFont font)
    {
        foreach (var dmg in _damageNumbers)
            dmg.Draw(spriteBatch, font);
    }

    // Simple floating damage number
    private class DamageNumber
    {
        private Vector2 _position;
        private float _timer = 0.7f;
        private readonly int _damage;
        public bool IsExpired => _timer <= 0;

        public DamageNumber(Vector2 position, int damage)
        {
            _position = position;
            _damage = damage;
        }

        public void Update(GameTime gameTime)
        {
            _timer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            _position.Y -= 30f * (float)gameTime.ElapsedGameTime.TotalSeconds;
        }

        public void Draw(SpriteBatch spriteBatch, SpriteFont font)
        {
            var color = Color.Lerp(Color.Red, Color.Transparent, 1 - (_timer / 0.7f));
            spriteBatch.DrawString(font, _damage.ToString(), _position, color);
        }
    }
}
