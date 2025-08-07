using Hearthvale.GameCode.Managers;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using nkast.Aether.Physics2D.Collision.Shapes;
using nkast.Aether.Physics2D.Dynamics;
using System;
using System.Collections.Generic;
using System.Linq;
using AetherVector2 = nkast.Aether.Physics2D.Common.Vector2;
using XnaVector2 = Microsoft.Xna.Framework.Vector2;

namespace Hearthvale.GameCode.Collision
{
    /// <summary>
    /// Collision detection system using Aether.Physics2D with XNA Vector2 for public APIs
    /// </summary>
    public class CollisionWorld : IDisposable
    {
        private readonly World _physicsWorld;
        private readonly Dictionary<Body, ICollisionActor> _bodyToActor = new();
        private readonly Dictionary<ICollisionActor, Body> _actorToBody = new();
        private bool _disposed = false;

        public IEnumerable<ICollisionActor> Actors => _bodyToActor.Values;

        public CollisionWorld(RectangleF bounds)
        {
            _physicsWorld = new World(AetherVector2.Zero);
            _physicsWorld.ContactManager.BeginContact = OnBeginContact;
            _physicsWorld.ContactManager.EndContact = OnEndContact;
        }

        /// <summary>
        /// Adds an actor to the collision world
        /// </summary>
        public void AddActor(ICollisionActor actor)
        {
            if (actor == null || _actorToBody.ContainsKey(actor))
                return;

            Body body = CreateBodyFromActor(actor);

            if (body != null)
            {
                _bodyToActor[body] = actor;
                _actorToBody[actor] = body;
            }
        }

        /// <summary>
        /// Removes an actor from the collision world
        /// </summary>
        public void RemoveActor(ICollisionActor actor)
        {
            if (actor == null || !_actorToBody.ContainsKey(actor))
                return;

            Body body = _actorToBody[actor];
            _physicsWorld.Remove(body);

            _bodyToActor.Remove(body);
            _actorToBody.Remove(actor);
        }

        /// <summary>
        /// Gets all actors at a specific location (XNA Vector2 input)
        /// </summary>
        public IEnumerable<ICollisionActor> GetActorsAt(XnaVector2 location)
        {
            var results = new List<ICollisionActor>();
            AetherVector2 worldPos = ConvertDisplayToSim(location);

            // Create a small AABB around the point for querying
            var queryAABB = new nkast.Aether.Physics2D.Collision.AABB(
                worldPos - new AetherVector2(0.01f, 0.01f),
                worldPos + new AetherVector2(0.01f, 0.01f)
            );

            _physicsWorld.QueryAABB((fixture) =>
            {
                if (_bodyToActor.TryGetValue(fixture.Body, out var actor))
                {
                    results.Add(actor);
                }
                return true;
            }, ref queryAABB);

            return results;
        }

        /// <summary>
        /// Gets all actors that intersect with the given bounds
        /// </summary>
        public IEnumerable<ICollisionActor> GetActorsInBounds(RectangleF bounds)
        {
            var results = new List<ICollisionActor>();

            var aabb = new nkast.Aether.Physics2D.Collision.AABB(
                ConvertDisplayToSim(new XnaVector2(bounds.Left, bounds.Top)),
                ConvertDisplayToSim(new XnaVector2(bounds.Right, bounds.Bottom))
            );

            _physicsWorld.QueryAABB((fixture) =>
            {
                if (_bodyToActor.TryGetValue(fixture.Body, out var actor))
                {
                    results.Add(actor);
                }
                return true;
            }, ref aabb);

            return results;
        }

        /// <summary>
        /// Updates the collision world
        /// </summary>
        public void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _physicsWorld.Step(deltaTime);
            UpdateActorBounds();
        }

        /// <summary>
        /// Checks if a position would collide with any actors of a specific type
        /// </summary>
        public bool WouldCollideWith<T>(RectangleF bounds) where T : ICollisionActor
        {
            return GetActorsInBounds(bounds).OfType<T>().Any();
        }

        /// <summary>
        /// Gets all actors of a specific type
        /// </summary>
        public IEnumerable<T> GetActorsOfType<T>() where T : ICollisionActor
        {
            return _bodyToActor.Values.OfType<T>();
        }

        /// <summary>
        /// Updates a specific actor's physics body position (XNA Vector2 input)
        /// </summary>
        public void UpdateActorPosition(ICollisionActor actor, XnaVector2 newPosition)
        {
            if (_actorToBody.TryGetValue(actor, out var body))
            {
                AetherVector2 physicsPosition = ConvertDisplayToSim(newPosition);
                body.Position = physicsPosition;
            }
        }

        /// <summary>
        /// Clears all actors from the collision world
        /// </summary>
        public void Clear()
        {
            var actorsToRemove = _actorToBody.Keys.ToList();
            foreach (var actor in actorsToRemove)
            {
                RemoveActor(actor);
            }
        }

        #region Private Methods - Aether.Physics2D specific

        private Body CreateBodyFromActor(ICollisionActor actor)
        {
            if (actor.Bounds is RectangleF rect)
            {
                AetherVector2 position = ConvertDisplayToSim(new XnaVector2(rect.X + rect.Width / 2, rect.Y + rect.Height / 2));
                AetherVector2 size = ConvertDisplayToSim(new XnaVector2(rect.Width, rect.Height));

                Body body = _physicsWorld.CreateRectangle(size.X, size.Y, 1f, position);

                if (actor is WallCollisionActor)
                {
                    body.BodyType = BodyType.Static;
                }
                else if (actor is ProjectileCollisionActor)
                {
                    body.BodyType = BodyType.Kinematic;
                }
                else
                {
                    body.BodyType = BodyType.Dynamic;
                }

                body.Tag = actor;
                return body;
            }
            else
            {
                var boundingRect = actor.Bounds.BoundingRectangle;
                AetherVector2 position = ConvertDisplayToSim(new XnaVector2(boundingRect.X + boundingRect.Width / 2, boundingRect.Y + boundingRect.Height / 2));
                AetherVector2 size = ConvertDisplayToSim(new XnaVector2(boundingRect.Width, boundingRect.Height));

                Body body = _physicsWorld.CreateRectangle(size.X, size.Y, 1f, position);
                body.BodyType = BodyType.Dynamic;
                body.Tag = actor;

                return body;
            }
        }

        private void UpdateActorBounds()
        {
            foreach (var kvp in _actorToBody)
            {
                var actor = kvp.Key;
                var body = kvp.Value;

                body.GetTransform(out var transform);
                var aabb = new nkast.Aether.Physics2D.Collision.AABB();

                for (int i = 0; i < body.FixtureList.Count; i++)
                {
                    var fixture = body.FixtureList[i];
                    var childAABB = new nkast.Aether.Physics2D.Collision.AABB();
                    fixture.Shape.ComputeAABB(out childAABB, ref transform, i);
                    aabb.Combine(ref childAABB);
                }

                XnaVector2 min = ConvertSimToDisplay(aabb.LowerBound);
                XnaVector2 max = ConvertSimToDisplay(aabb.UpperBound);

                actor.Bounds = new RectangleF(min.X, min.Y, max.X - min.X, max.Y - min.Y);
            }
        }

        private bool OnBeginContact(nkast.Aether.Physics2D.Dynamics.Contacts.Contact contact)
        {
            var bodyA = contact.FixtureA.Body;
            var bodyB = contact.FixtureB.Body;

            if (_bodyToActor.TryGetValue(bodyA, out var actorA) &&
                _bodyToActor.TryGetValue(bodyB, out var actorB))
            {
                contact.GetWorldManifold(out AetherVector2 normal, out _);
                AetherVector2 aetherPenetrationVector = normal * contact.Manifold.LocalNormal.Length();

                // Convert to XNA Vector2 for the event args
                XnaVector2 penetrationVector = ConvertAetherToXna(aetherPenetrationVector);

                actorA.OnCollision(new CollisionEventArgs(actorB, penetrationVector));
                actorB.OnCollision(new CollisionEventArgs(actorA, -penetrationVector));
            }

            return true;
        }

        private void OnEndContact(nkast.Aether.Physics2D.Dynamics.Contacts.Contact contact)
        {
            // Handle collision end if needed
        }

        private AetherVector2 ConvertDisplayToSim(XnaVector2 displayCoords)
        {
            const float scale = 64f; // 64 pixels per meter
            return new AetherVector2(displayCoords.X / scale, displayCoords.Y / scale);
        }

        private XnaVector2 ConvertSimToDisplay(AetherVector2 simCoords)
        {
            const float scale = 64f; // 64 pixels per meter
            return new XnaVector2(simCoords.X * scale, simCoords.Y * scale);
        }

        private XnaVector2 ConvertAetherToXna(AetherVector2 aetherVector)
        {
            return new XnaVector2(aetherVector.X, aetherVector.Y);
        }

        #endregion

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                Clear();
                // Aether.Physics2D World doesn't implement IDisposable
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Interface for collision actors - uses MonoGame.Extended shapes
    /// </summary>
    public interface ICollisionActor
    {
        IShapeF Bounds { get; set; }
        void OnCollision(CollisionEventArgs collisionInfo);
    }

    /// <summary>
    /// Collision event arguments using XNA Vector2 for consistency
    /// </summary>
    public class CollisionEventArgs : EventArgs
    {
        public ICollisionActor Other { get; set; }
        public XnaVector2 PenetrationVector { get; set; }

        public CollisionEventArgs(ICollisionActor other, XnaVector2 penetrationVector)
        {
            Other = other;
            PenetrationVector = penetrationVector;
        }
    }
}