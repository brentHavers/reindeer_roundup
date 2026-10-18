using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SoManyPixels.ArcEngine.A2D;
using SoManyPixels.ArcEngine.A2D.Components;
using SoManyPixels.ArcEngine.A2D.Entities;
using SoManyPixels.ArcEngine.Base.DataTypes;
using SoManyPixels.ReindeerRoundup.Components;
using SoManyPixels.ArcEngine.A2D.Systems;

namespace SoManyPixels.ReindeerRoundup.Game
{
    public enum PlayerThumbstick
    {
        Left,
        Right
    }

    public class PlayerScript : A2DScriptComponent
    {
        // Tunables in pixel units for frame-rate independent movement.
        const float X_SPEED_MAX = 550.0f;       // px/s
        const float Y_SPEED_MAX = 550.0f;       // px/s
        const float X_ACCELERATION = 2250.0f;   // px/s^2
        const float Y_ACCELERATION = 2250.0f;   // px/s^2
        const float X_DECELERATION = 600.0f;   // px/s^2
        const float Y_DECELERATION = 600.0f;   // px/s^2
        A2DIntVector2 collisionActivatePosition = A2DIntVector2.Zero;

        readonly int _gamePadSlot;
        readonly PlayerThumbstick _thumbstick;
        A2DInputSystem _inputSystem;

        int _collisionCount = 0;
        float _velocityX = 0.0f;
        float _velocityY = 0.0f;
        float _positionX = 0.0f;
        float _positionY = 0.0f;
        bool _movementInitialized = false;

        // gamePadSlot is 0..3 (controller 1..4). For slot 0, the input system falls back to
        // WASD (left stick) or IJKL (right stick) when no controller is connected.
        public PlayerScript(A2DEntityWorld world, int gamePadSlot, PlayerThumbstick thumbstick)
        {
            _entityWorld = world;
            _gamePadSlot = gamePadSlot;
            _thumbstick = thumbstick;
        }

        public override bool OnUpdate(float elapsedtime)
        {
            EnsureInputSystem();

            A2DTransformComponent transformComponent = GetComponent<A2DTransformComponent>();
            A2DFloatVector stick = GetMovementStick();
            float dt = elapsedtime / 1000.0f;

            InitializeMovementState(transformComponent);

            float targetVelocityX = stick.X * X_SPEED_MAX;
            float targetVelocityY = -stick.Y * Y_SPEED_MAX;

            _velocityX = MoveToward(
                _velocityX,
                targetVelocityX,
                (Math.Abs(targetVelocityX) > 0.0f ? X_ACCELERATION : X_DECELERATION) * dt);
            _velocityY = MoveToward(
                _velocityY,
                targetVelocityY,
                (Math.Abs(targetVelocityY) > 0.0f ? Y_ACCELERATION : Y_DECELERATION) * dt);

            _positionX += _velocityX * dt;
            _positionY += _velocityY * dt;

            transformComponent.SetDirection(_velocityX, _velocityY);
            transformComponent.PositionX = (int)Math.Round(_positionX);
            transformComponent.PositionY = (int)Math.Round(_positionY);

            _collisionCount = 0;

            return true; // TODO: return type/value?
        }

        A2DFloatVector GetMovementStick()
        {
            return _thumbstick == PlayerThumbstick.Left
                ? _inputSystem.GetLeftStick(_gamePadSlot)
                : _inputSystem.GetRightStick(_gamePadSlot);
        }

        void EnsureInputSystem()
        {
            if (_inputSystem != null)
            {
                return;
            }

            _inputSystem = _entityWorld.GetSystem<A2DInputSystem>();
            if (_inputSystem == null)
            {
                throw new InvalidOperationException(
                    "PlayerScript requires A2DInputSystem to be registered on the entity world before update.");
            }
        }

        private static float MoveToward(float currentValue, float targetValue, float maxDelta)
        {
            if (currentValue < targetValue)
            {
                return Math.Min(currentValue + maxDelta, targetValue);
            }

            if (currentValue > targetValue)
            {
                return Math.Max(currentValue - maxDelta, targetValue);
            }

            return currentValue;
        }

        private void InitializeMovementState(A2DTransformComponent transformComponent)
        {
            if (_movementInitialized)
            {
                return;
            }

            _positionX = transformComponent.PositionX;
            _positionY = transformComponent.PositionY;
            _velocityX = transformComponent.DirectionX;
            _velocityY = transformComponent.DirectionY;
            _movementInitialized = true;
        }

        private void SyncInternalMovementState(A2DTransformComponent transformComponent)
        {
            _positionX = transformComponent.PositionX;
            _positionY = transformComponent.PositionY;
        }

        public override bool OnCollisionActivate(A2DCollision collision)
        {
            if (collision.IncomingCollider.Name == "EnemyBullet1")
            {
                A2DPlayerStatsComponent playerStatsComponent = GetComponent<A2DPlayerStatsComponent>();

                if (playerStatsComponent != null)
                {
                    playerStatsComponent.HealthValue--;
                }
            }

            if (collision.IncomingCollider.Kind == CollisionKinds.Wall)
            {
                HandleWallCollision(collision);
            }

            if (!collision.IncomingCollider.OwnerEntityName.StartsWith("Friendly"))
            {
                _collisionCount++;

                //collisionActivatePosition.X = _owner.GetComponent<A2DTransformComponent>().Position.X;
                //collisionActivatePosition.Y = _owner.GetComponent<A2DTransformComponent>().Position.Y;

                //collisionActivatePosition.X = collision.IncomingEntity.GetComponent<A2DTransformComponent>().PositionX - collision.Entity.GetComponent<A2DBoundingBoxComponent>().BoundingBox.Width;
                //collisionActivatePosition.Y = collision.IncomingEntity.GetComponent<A2DTransformComponent>().PositionY - collision.Entity.GetComponent<A2DBoundingBoxComponent>().BoundingBox.Height;
            }

            return true;
        }

        public override bool OnCollisionDeactivate(A2DCollision collision)
        {
            //if (collision.IncomingCollider.Owner.Name.StartsWith("Wall"))
            //{
            //    if (collision.Collider.Name.Equals("BoundingBoxLeft"))
            //    {
            //        A2DTransformComponent transformComponent = _owner.GetComponent<A2DTransformComponent>();

            //        A2DTransformComponent incomingTransformComponent = collision.IncomingCollider.Owner.GetComponent<A2DTransformComponent>();
            //        A2DBoundingBoxComponent incomingBoundingBox = collision.IncomingCollider.Owner.GetComponent<A2DBoundingBoxComponent>();

            //        if (transformComponent.DirectionX <= 0)
            //        {
            //            transformComponent.DirectionX = 0;

            //            transformComponent.PositionX = incomingTransformComponent.PositionX + incomingBoundingBox.BoundingBox.Width;
            //        }

            //        bool here = true; // check collision.Entity
            //    }
            //}

            return true;
        }

        public override bool OnCollisionPersist(A2DCollision collision)
        {
            if (collision.IncomingCollider.Kind == CollisionKinds.Wall)
            {
                HandleWallCollision(collision);
            }

            return true;
        }

        // Resolves a wall overlap by pushing the player back to whichever side of the
        // wall the colliding box's center is currently on, and zeroing the velocity
        // component pointing into the wall. The collider name (BoundingBoxLeft/Right/
        // Top/Bottom) only picks the axis; the snap direction is not derived from it
        // because a box can overlap a wall from either side, which previously moved
        // the ship through thin walls to the far side (off screen).
        void HandleWallCollision(A2DCollision collision)
        {
            A2DTransformComponent transformComponent = GetComponent<A2DTransformComponent>();
            A2DBoundingBoxComponent ourBoundingBox = (A2DBoundingBoxComponent)collision.Collider;
            A2DTransformComponent wallTransform = _entityWorld.GetComponent<A2DTransformComponent>(collision.IncomingCollider.OwnerId);
            A2DBoundingBoxComponent wallBoundingBox = _entityWorld.GetComponent<A2DBoundingBoxComponent>(collision.IncomingCollider.OwnerId);

            if (transformComponent == null || ourBoundingBox == null || wallTransform == null || wallBoundingBox == null)
            {
                return;
            }

            A2DIntRectangle ourBox = ourBoundingBox.BoundingBox;
            A2DIntRectangle wallBox = wallBoundingBox.BoundingBox;

            int wallX = wallTransform.PositionX + wallBox.X;
            int wallY = wallTransform.PositionY + wallBox.Y;

            bool horizontalContact = collision.Collider.Name.Equals("BoundingBoxLeft") || collision.Collider.Name.Equals("BoundingBoxRight");
            bool verticalContact = collision.Collider.Name.Equals("BoundingBoxTop") || collision.Collider.Name.Equals("BoundingBoxBottom");

            if (horizontalContact)
            {
                int ourCenterX = transformComponent.PositionX + ourBox.X + (ourBox.Width / 2);
                int wallCenterX = wallX + (wallBox.Width / 2);

                if (ourCenterX <= wallCenterX)
                {
                    if (transformComponent.DirectionX > 0)
                    {
                        transformComponent.DirectionX = 0;
                        _velocityX = 0.0f;
                    }

                    transformComponent.PositionX = wallX - ourBox.X - ourBox.Width;
                }
                else
                {
                    if (transformComponent.DirectionX < 0)
                    {
                        transformComponent.DirectionX = 0;
                        _velocityX = 0.0f;
                    }

                    transformComponent.PositionX = wallX + wallBox.Width - ourBox.X;
                }

                SyncInternalMovementState(transformComponent);
            }

            if (verticalContact)
            {
                int ourCenterY = transformComponent.PositionY + ourBox.Y + (ourBox.Height / 2);
                int wallCenterY = wallY + (wallBox.Height / 2);

                if (ourCenterY <= wallCenterY)
                {
                    if (transformComponent.DirectionY > 0)
                    {
                        transformComponent.DirectionY = 0;
                        _velocityY = 0.0f;
                    }

                    transformComponent.PositionY = wallY - ourBox.Y - ourBox.Height;
                }
                else
                {
                    if (transformComponent.DirectionY < 0)
                    {
                        transformComponent.DirectionY = 0;
                        _velocityY = 0.0f;
                    }

                    transformComponent.PositionY = wallY + wallBox.Height - ourBox.Y;
                }

                SyncInternalMovementState(transformComponent);
            }
        }

        public override void SendMessage(string message, object sender, object param)
        {
            if (message == "powerup")
            {
                GetComponent<WeaponBullet>().BulletCount += 2;
            }

            base.SendMessage(message, sender, param);
        }
    }
}
