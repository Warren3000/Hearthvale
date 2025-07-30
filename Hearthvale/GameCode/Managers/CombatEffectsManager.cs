using Hearthvale.GameCode.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Hearthvale.GameCode.Managers;
public class CombatEffectsManager
{
    private readonly Camera2D _camera;
    private readonly List<CombatTextEffect> _combatTexts = new();

    public CombatEffectsManager(Camera2D camera)
    {
        _camera = camera;
    }

    public void PlayHitEffects(Vector2 position, int damage)
    {
        // Screen shake
        _camera.Shake(0.08f, 1.5f);

        // Damage number as CombatTextEffect (red, shorter duration)
        _combatTexts.Add(new CombatTextEffect(
            start: position,
            end: position - new Vector2(0, 32),
            duration: 0.7f,
            text: damage.ToString(),
            color: Color.Red
        ));
    }

    public void ShowCombatText(Vector2 position, string text, Color color, float duration = 0.8f)
    {
        var effect = new CombatTextEffect(
            start: position,
            end: position - new Vector2(0, 32), // floats up
            duration: duration,
            text: text,
            color: color
        );
        _combatTexts.Add(effect);
    }

    public void Update(GameTime gameTime)
    {
        float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
        for (int i = _combatTexts.Count - 1; i >= 0; i--)
        {
            _combatTexts[i].Update(delta);
            if (_combatTexts[i].IsFinished)
                _combatTexts.RemoveAt(i);
        }
    }

    public void Draw(SpriteBatch spriteBatch, SpriteFont font)
    {
        foreach (var effect in _combatTexts)
            effect.Draw(spriteBatch, font);
    }   

    public class CombatTextEffect
    {
        public Vector2 StartPosition;
        public Vector2 EndPosition;
        public float Duration;
        public float Elapsed;
        public string Text;
        public Color Color;
        public float StartAlpha;
        public float EndAlpha;

        public CombatTextEffect(Vector2 start, Vector2 end, float duration, string text, Color color, float startAlpha = 1f, float endAlpha = 0f)
        {
            StartPosition = start;
            EndPosition = end;
            Duration = duration;
            Text = text;
            Color = color;
            StartAlpha = startAlpha;
            EndAlpha = endAlpha;
            Elapsed = 0f;
        }

        public CombatTextEffect(Vector2 position, int damage)
            : this(position, position - new Vector2(0, 32), 0.7f, damage.ToString(), Color.Red)
        {
        }

        public bool IsFinished => Elapsed >= Duration;

        public void Update(float deltaTime)
        {
            Elapsed += deltaTime;
        }

        public void Draw(SpriteBatch spriteBatch, SpriteFont font)
        {
            float t = MathHelper.Clamp(Elapsed / Duration, 0f, 1f);

            // Smooth vertical float using sine ease
            float verticalOffset = -32f * MathF.Sin(t * MathF.PI * 0.5f);

            // Optional: small horizontal wobble for more natural look
            float horizontalOffset = 4f * MathF.Sin(t * MathF.PI * 2f);

            Vector2 position = StartPosition + new Vector2(horizontalOffset, verticalOffset);

            float alpha = MathHelper.Lerp(StartAlpha, EndAlpha, t);
            spriteBatch.DrawString(font, Text, position, Color * alpha);
        }
    }
}
