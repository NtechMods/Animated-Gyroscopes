using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using System;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace nukeguardRotatingGyroSmall
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Gyro), false, "LBAnimGyro")]
    public class RotatingGyroLarge : MyGameLogicComponent
    {
        public IMyGyro LGyro_block;
        public MyEntitySubpart LGyroSubpart;
        private float HingePosX = 0f; // Hinge position on the X axis. 0 is center.
        private float HingePosY = -0.051649f; // Hinge position on the Y axis. 0 is center.
        private float HingePosZ = -0.088645f; // Hinge position on the Z axis. 0 is center.
        private float RotX = 0.05f; // Rotation on the X axis. 0 is no rotation.
        private float RotY = 0f; // Rotation on the Y axis. 0 is no rotation.
        private float RotZ = 0f; // Rotation on the Z axis. 0 is no rotation.
        public bool InitSubpart = true;

        // These are static as their identities shouldnt change across different instances
        private Matrix _rotMatrixX;
        private Matrix _rotMatrixY;
        private Matrix _rotMatrixZ;
		private Matrix newRotMatrixX;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
            LGyro_block = (IMyGyro)Entity;

            // Init the rot matricies here as you shouldnt need to recompute them
            // If you want an angle to be zero, use Matrix.Identity instead :)
            _rotMatrixX = Matrix.CreateRotationX(RotX);
            _rotMatrixY = Matrix.CreateRotationY(RotY);
            _rotMatrixZ = Matrix.CreateRotationY(RotZ);
        }

        public override void Close()
        {
            NeedsUpdate = MyEntityUpdateEnum.NONE;
        }


        public override void UpdateBeforeSimulation()
        {
            try
            {
                if (!LGyro_block.IsFunctional) return;                // Ignore damaged or build progress blocks.
                if (LGyro_block.CubeGrid.Physics == null) return;     // Ignore ghost grids (projections).
				if (LGyro_block.IsWorking) newRotMatrixX = _rotMatrixX;
                else newRotMatrixX = Matrix.Identity;
                if (InitSubpart)
                {
                    LGyro_block.TryGetSubpart("AnimGyro", out LGyroSubpart);
                    InitSubpart = false;
                }
                var distanceFromCameraToBlock = Vector3D.DistanceSquared(LGyroSubpart.PositionComp.WorldAABB.Center, MyAPIGateway.Session.Camera.Position) < 10000;
                if (!MyAPIGateway.Utilities.IsDedicated && distanceFromCameraToBlock)
                {
                    var blockCam = LGyroSubpart.PositionComp.WorldVolume;
                    if (MyAPIGateway.Session.Camera.IsInFrustum(ref blockCam) && LGyro_block.IsWorking) Matrix.CreateRotationX(RotX);
                }
                // Checks if subpart is removed (i.e. when changing block color).
                if (LGyroSubpart.Closed.Equals(true)) ResetLostSubpart();

                if (LGyroSubpart != null)
                {
                    var hingePos = new Vector3(HingePosX, HingePosY, HingePosZ); // This defines the location of a new pivot point.
                    var MatrixTransl1 = Matrix.CreateTranslation(-(hingePos));
                    var MatrixTransl2 = Matrix.CreateTranslation(hingePos);
                    var rotMatrix = LGyroSubpart.PositionComp.LocalMatrix;
                    // rotMatrix *= (MatrixTransl1 * (Matrix.CreateRotationX(RotX) * Matrix.CreateRotationY(RotY) * Matrix.CreateRotationZ(RotZ)) * MatrixTransl2);
                    rotMatrix *= (MatrixTransl1 * newRotMatrixX * _rotMatrixY * _rotMatrixZ * MatrixTransl2);
                    LGyroSubpart.PositionComp.LocalMatrix = rotMatrix;
                }
            }
            catch (Exception e)
            {
                // This is for your benefit. Remove or change with your logging option before publishing.
                MyAPIGateway.Utilities.ShowNotification("Error: " + e.Message, 16);
            }


            /*catch (Exception e)
            {
                Logging.Instance.WriteLine("Error: " + e.Message);
            }*/
        }
        

        public override void UpdateAfterSimulation()
        {
            if (MyAPIGateway.Utilities.IsDedicated) return;
            MyEntitySubpart LGyrosubpart;
            if (LGyro_block != null && LGyro_block.IsWorking && LGyro_block.IsFunctional && LGyro_block.TryGetSubpart("AnimGyro", out LGyrosubpart))
            {
                var _emcolor = LGyro_block.GyroPower;
                LGyrosubpart.SetEmissiveParts("Emissive", Color.Green, _emcolor);
            }
            else if (LGyro_block != null && LGyro_block.TryGetSubpart("AnimGyro", out LGyrosubpart))
            {
                var _emoff = LGyro_block.GyroPower;
                LGyrosubpart.SetEmissiveParts("Emissive", Color.Red, _emoff);
            }

        }

            private void ResetLostSubpart()
        {
            LGyroSubpart.Subparts.Clear();
            LGyro_block.TryGetSubpart("AnimGyro", out LGyroSubpart);
        }
    }
}