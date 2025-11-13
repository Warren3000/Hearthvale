using Hearthvale.GameCode.Entities;
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
    public class CollisionWorld : IDisposable
    {
        private readonly World _physicsWorld;
        private readonly Dictionary<Body, ICollisionActor> _bodyToActor = [];
        private readonly Dictionary<ICollisionActor, Body> _actorToBody = [];
        private readonly Dictionary<ICollisionActor, XnaVector2> _pendingSeparations = new();
        private bool _disposed = false;

        // Limit how far we separate actors in a single physics step to prevent extreme bounce.
        private const float MaxSeparationPerContact = 4f;
        private const float MaxAccumulatedSeparation = MaxSeparationPerContact * 3f;

        public IEnumerable<ICollisionActor> Actors => _bodyToActor.Values;

        public CollisionWorld(RectangleF bounds)
        {
            _physicsWorld = new World(AetherVector2.Zero);
            _physicsWorld.ContactManager.BeginContact = OnBeginContact;
            _physicsWorld.ContactManager.EndContact = OnEndContact;
        }

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

        public void RemoveActor(ICollisionActor actor)
        {
            if (actor == null || !_actorToBody.ContainsKey(actor))
                return;

            Body body = _actorToBody[actor];
            _physicsWorld.Remove(body);

            _bodyToActor.Remove(body);
            _actorToBody.Remove(actor);
        }

        public IEnumerable<ICollisionActor> GetActorsAt(XnaVector2 location)
        {
            var results = new List<ICollisionActor>();
            AetherVector2 worldPos = ConvertDisplayToSim(location);

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

        public void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _physicsWorld.Step(deltaTime);
            ApplyPendingSeparations();
            UpdateActorBounds();
        }

        public bool WouldCollideWith<T>(RectangleF bounds) where T : ICollisionActor
        {
            return GetActorsInBounds(bounds).OfType<T>().Any();
        }

        public IEnumerable<T> GetActorsOfType<T>() where T : ICollisionActor
        {
            return _bodyToActor.Values.OfType<T>();
        }

        public void UpdateActorPosition(ICollisionActor actor, XnaVector2 newPosition)
        {
            if (_actorToBody.TryGetValue(actor, out var body))
            {
                RectangleF resolvedBounds = ResolveActorBounds(actor);
                if (!IsValidBounds(resolvedBounds))
                {
                    return;
                }

                if (actor is not IDynamicCollisionActor)
                {
                    resolvedBounds = new RectangleF(
                        newPosition.X - resolvedBounds.Width * 0.5f,
                        newPosition.Y - resolvedBounds.Height * 0.5f,
                        resolvedBounds.Width,
                        resolvedBounds.Height);
                }

                var center = new XnaVector2(resolvedBounds.Center.X, resolvedBounds.Center.Y);
                body.Position = ConvertDisplayToSim(center);
                SyncBodyFixture(actor, body, resolvedBounds);
                actor.Bounds = resolvedBounds;
            }
        }

        public void Clear()
        {
            var actorsToRemove = _actorToBody.Keys.ToList();
            foreach (var actor in actorsToRemove)
            {
                RemoveActor(actor);
            }
        }

        #region Private Methods

        private Body CreateBodyFromActor(ICollisionActor actor)
        {
            RectangleF resolvedBounds = ResolveActorBounds(actor);
            if (!IsValidBounds(resolvedBounds))
            {
                return null;
            }

            actor.Bounds = resolvedBounds;

            AetherVector2 position = ConvertDisplayToSim(new XnaVector2(resolvedBounds.Center.X, resolvedBounds.Center.Y));
            AetherVector2 size = ConvertDisplayToSim(new XnaVector2(resolvedBounds.Width, resolvedBounds.Height));

            Body body = _physicsWorld.CreateRectangle(size.X, size.Y, 1f, position);
            ApplyBodyType(body, actor);
            body.Tag = actor;
            return body;
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

        private RectangleF ResolveActorBounds(ICollisionActor actor)
        {
            if (actor is IDynamicCollisionActor dynamicActor)
            {
                var bounds = dynamicActor.GetCurrentBounds();
                if (IsValidBounds(bounds))
                {
                    return bounds;
                }
            }

            if (actor.Bounds is RectangleF rectF && IsValidBounds(rectF))
            {
                return rectF;
            }

            if (actor.Bounds != null)
            {
                var rect = actor.Bounds.BoundingRectangle;
                var rectangleF = new RectangleF(rect.X, rect.Y, rect.Width, rect.Height);
                if (IsValidBounds(rectangleF))
                {
                    return rectangleF;
                }
            }

            return actor.CalculateInitialBounds();
        }

        private static bool IsValidBounds(RectangleF rect)
        {
            return rect.Width > 0 && rect.Height > 0 &&
                   !float.IsNaN(rect.X) && !float.IsNaN(rect.Y) &&
                   !float.IsNaN(rect.Width) && !float.IsNaN(rect.Height) &&
                   !float.IsInfinity(rect.Width) && !float.IsInfinity(rect.Height);
        }

        private void SyncBodyFixture(ICollisionActor actor, Body body, RectangleF desiredBounds)
        {
            var desiredSize = ConvertDisplayToSim(new XnaVector2(desiredBounds.Width, desiredBounds.Height));

            if (body.FixtureList.Count == 0)
            {
                CreateRectangleFixture(body, desiredBounds, actor);
                return;
            }

            var fixture = body.FixtureList[0];
            if (fixture.Shape is PolygonShape polygon && polygon.Vertices.Count >= 3)
            {
                float minX = float.PositiveInfinity;
                float maxX = float.NegativeInfinity;
                float minY = float.PositiveInfinity;
                float maxY = float.NegativeInfinity;

                for (int i = 0; i < polygon.Vertices.Count; i++)
                {
                    var v = polygon.Vertices[i];
                    if (v.X < minX) minX = v.X;
                    if (v.X > maxX) maxX = v.X;
                    if (v.Y < minY) minY = v.Y;
                    if (v.Y > maxY) maxY = v.Y;
                }

                float currentWidth = maxX - minX;
                float currentHeight = maxY - minY;

                if (MathF.Abs(currentWidth - desiredSize.X) > 0.0005f ||
                    MathF.Abs(currentHeight - desiredSize.Y) > 0.0005f)
                {
                    body.Remove(fixture);
                    CreateRectangleFixture(body, desiredBounds, actor);
                }
            }
            else
            {
                var fixtures = body.FixtureList.ToArray();
                for (int i = 0; i < fixtures.Length; i++)
                {
                    body.Remove(fixtures[i]);
                }
                CreateRectangleFixture(body, desiredBounds, actor);
            }
        }

        private void CreateRectangleFixture(Body body, RectangleF bounds, ICollisionActor actor)
        {
            var size = ConvertDisplayToSim(new XnaVector2(bounds.Width, bounds.Height));
            body.CreateRectangle(size.X, size.Y, 1f, new AetherVector2(0f, 0f));
            ApplyBodyType(body, actor);
            body.ResetMassData();
        }

        private static void ApplyBodyType(Body body, ICollisionActor actor)
        {
            if (actor is WallCollisionActor || actor is ChestCollisionActor)
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
        }

        private bool OnBeginContact(nkast.Aether.Physics2D.Dynamics.Contacts.Contact contact)
        {
            var bodyA = contact.FixtureA.Body;
            var bodyB = contact.FixtureB.Body;

            if (_bodyToActor.TryGetValue(bodyA, out var actorA) &&
                _bodyToActor.TryGetValue(bodyB, out var actorB))
            {
                var (pushA, pushB) = CalculateRepulsionVectors(actorA, actorB);

                QueueSeparation(actorA, pushA);
                QueueSeparation(actorB, pushB);

                actorA.OnCollision(new CollisionEventArgs(actorB, pushA));
                actorB.OnCollision(new CollisionEventArgs(actorA, pushB));
            }

            return true;
        }

        private void OnEndContact(nkast.Aether.Physics2D.Dynamics.Contacts.Contact contact)
        {
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

        private void QueueSeparation(ICollisionActor actor, XnaVector2 push)
        {
            if (actor == null || push == XnaVector2.Zero)
            {
                return;
            }

            var limitedPush = ClampSeparationMagnitude(push, MaxSeparationPerContact);

            if (_pendingSeparations.TryGetValue(actor, out var existing))
            {
                var combined = existing + limitedPush;
                _pendingSeparations[actor] = ClampSeparationMagnitude(combined, MaxAccumulatedSeparation);
            }
            else
            {
                _pendingSeparations[actor] = ClampSeparationMagnitude(limitedPush, MaxAccumulatedSeparation);
            }
        }

        private static XnaVector2 ClampSeparationMagnitude(XnaVector2 value, float maxMagnitude)
        {
            if (value == XnaVector2.Zero || maxMagnitude <= 0f)
            {
                return value;
            }

            float maxMagnitudeSquared = maxMagnitude * maxMagnitude;
            float lengthSquared = value.LengthSquared();
            if (lengthSquared <= maxMagnitudeSquared)
            {
                return value;
            }

            float length = MathF.Sqrt(lengthSquared);
            if (length <= float.Epsilon)
            {
                return XnaVector2.Zero;
            }

            return value * (maxMagnitude / length);
        }

        private (XnaVector2 pushA, XnaVector2 pushB) CalculateRepulsionVectors(ICollisionActor actorA, ICollisionActor actorB)
        {
            if (actorA is ProjectileCollisionActor || actorB is ProjectileCollisionActor)
            {
                return (XnaVector2.Zero, XnaVector2.Zero);
            }

            if (!TryGetRectangle(actorA, out var boundsA) ||
                !TryGetRectangle(actorB, out var boundsB))
            {
                return (XnaVector2.Zero, XnaVector2.Zero);
            }

            if (!boundsA.Intersects(boundsB))
            {
                return (XnaVector2.Zero, XnaVector2.Zero);
            }

            var intersection = RectangleF.Intersection(boundsA, boundsB);
            if (intersection.Width <= 0f || intersection.Height <= 0f)
            {
                return (XnaVector2.Zero, XnaVector2.Zero);
            }

            bool aDynamic = IsDynamicActor(actorA);
            bool bDynamic = IsDynamicActor(actorB);

            if (!aDynamic && !bDynamic)
            {
                return (XnaVector2.Zero, XnaVector2.Zero);
            }

            const float SeparationPadding = 0.25f;

            var centerA = boundsA.Center;
            var centerB = boundsB.Center;

            if (intersection.Width < intersection.Height)
            {
                float amount = intersection.Width + SeparationPadding;
                var direction = centerA.X <= centerB.X ? new XnaVector2(-1f, 0f) : new XnaVector2(1f, 0f);
                return BuildPushVectors(direction, amount, aDynamic, bDynamic);
            }
            else
            {
                float amount = intersection.Height + SeparationPadding;
                var direction = centerA.Y <= centerB.Y ? new XnaVector2(0f, -1f) : new XnaVector2(0f, 1f);
                return BuildPushVectors(direction, amount, aDynamic, bDynamic);
            }
        }

        private static (XnaVector2 pushA, XnaVector2 pushB) BuildPushVectors(XnaVector2 direction, float amount, bool aDynamic, bool bDynamic)
        {
            if (direction == XnaVector2.Zero || amount <= 0f)
            {
                return (XnaVector2.Zero, XnaVector2.Zero);
            }

            amount = MathF.Min(amount, MaxSeparationPerContact);

            if (aDynamic && bDynamic)
            {
                var push = ClampSeparationMagnitude(direction * (amount * 0.5f), MaxSeparationPerContact);
                return (push, -push);
            }

            if (aDynamic)
            {
                var push = ClampSeparationMagnitude(direction * amount, MaxSeparationPerContact);
                return (push, XnaVector2.Zero);
            }

            if (bDynamic)
            {
                var push = ClampSeparationMagnitude(-direction * amount, MaxSeparationPerContact);
                return (XnaVector2.Zero, push);
            }

            return (XnaVector2.Zero, XnaVector2.Zero);
        }

        private void ApplyPendingSeparations()
        {
            if (_pendingSeparations.Count == 0)
            {
                return;
            }

            foreach (var kvp in _pendingSeparations)
            {
                ApplySeparation(kvp.Key, kvp.Value);
            }

            _pendingSeparations.Clear();
        }

        private void ApplySeparation(ICollisionActor actor, XnaVector2 push)
        {
            if (push == XnaVector2.Zero)
            {
                return;
            }

            switch (actor)
            {
                case PlayerCollisionActor playerActor when playerActor.Player != null:
                    HandleDynamicCharacterSeparation(playerActor.Player, push);
                    SyncDynamicActor(playerActor);
                    break;
                case NpcCollisionActor npcActor when npcActor.Npc != null:
                    HandleDynamicCharacterSeparation(npcActor.Npc, push);
                    SyncDynamicActor(npcActor);
                    break;
                default:
                    if (_actorToBody.TryGetValue(actor, out var body))
                    {
                        body.Position += ConvertDisplayToSim(push);
                        body.LinearVelocity = AetherVector2.Zero;
                        body.Awake = true;
                        UpdateActorBoundsFromBody(actor, body);
                    }
                    break;
            }
        }

        private static void HandleDynamicCharacterSeparation(Character character, XnaVector2 push)
        {
            if (character == null || push == XnaVector2.Zero)
            {
                return;
            }

            var collision = character.CollisionComponent;
            if (collision != null)
            {
                var target = character.Position + push;
                if (!collision.TryMove(target))
                {
                    collision.CancelKnockbackAlong(push);
                }
                return;
            }

            character.SetPosition(character.Position + push);
        }

        private void SyncDynamicActor(ICollisionActor actor)
        {
            var updatedBounds = ResolveActorBounds(actor);
            actor.Bounds = updatedBounds;

            if (_actorToBody.TryGetValue(actor, out var body))
            {
                var center = new XnaVector2(updatedBounds.Center.X, updatedBounds.Center.Y);
                body.Position = ConvertDisplayToSim(center);
                body.LinearVelocity = AetherVector2.Zero;
                body.Awake = true;
            }
        }

        private static bool IsDynamicActor(ICollisionActor actor)
        {
            return actor is IDynamicCollisionActor;
        }

        private static bool TryGetRectangle(ICollisionActor actor, out RectangleF rectangle)
        {
            if (actor?.Bounds is RectangleF rect)
            {
                rectangle = rect;
                return true;
            }

            if (actor?.Bounds != null)
            {
                var bounds = actor.Bounds.BoundingRectangle;
                rectangle = new RectangleF(bounds.X, bounds.Y, bounds.Width, bounds.Height);
                return true;
            }

            rectangle = RectangleF.Empty;
            return false;
        }

        private void UpdateActorBoundsFromBody(ICollisionActor actor, Body body)
        {
            body.GetTransform(out var transform);
            var aabb = new nkast.Aether.Physics2D.Collision.AABB();

            for (int i = 0; i < body.FixtureList.Count; i++)
            {
                var fixture = body.FixtureList[i];
                var childAABB = new nkast.Aether.Physics2D.Collision.AABB();
                fixture.Shape.ComputeAABB(out childAABB, ref transform, i);
                aabb.Combine(ref childAABB);
            }

            var min = ConvertSimToDisplay(aabb.LowerBound);
            var max = ConvertSimToDisplay(aabb.UpperBound);

            actor.Bounds = new RectangleF(min.X, min.Y, max.X - min.X, max.Y - min.Y);
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
                _disposed = true;
            }
        }
    }

    public interface ICollisionActor
    {
        IShapeF Bounds { get; set; }
        void OnCollision(CollisionEventArgs collisionInfo);
        /// <summary>
        /// Calculates and returns the initial bounds of the actor.
        /// </summary>
        RectangleF CalculateInitialBounds();
    }

    public interface IDynamicCollisionActor : ICollisionActor
    {
        /// <summary>
        /// Returns the current world-space axis-aligned bounds for this actor.
        /// </summary>
        RectangleF GetCurrentBounds();
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