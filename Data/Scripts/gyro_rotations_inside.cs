using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using System;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace nukeguardRotatingGyro
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Gyro), false, "LBAnimGyroInside")]
    public class RotatingGyro : MyGameLogicComponent
    {
        public IMyGyro Gyro_block;
        public MyEntitySubpart GyroSubpart;
        private float HingePosX = 0f; // Hinge position on the X axis. 0 is center.
        private float HingePosY = -0.051649f; // Hinge position on the Y axis. 0 is center.
        private float HingePosZ = -0.088645f; // Hinge position on the Z axis. 0 is center.
        private float RotX = 0f; // Rotation on the X axis. 0 is no rotation.
        private float RotY = 0.18f; // Rotation on the Y axis. 0 is no rotation.
        private float RotZ = 0f; // Rotation on the Z axis. 0 is no rotation.
        public bool InitSubpart = true;

        // These are static as their identities shouldnt change across different instances
        private Matrix _rotMatrixX;
        private Matrix _rotMatrixY;
        private Matrix _rotMatrixZ;
		private Matrix newRotMatrixY;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
            Gyro_block = (IMyGyro)Entity;

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
                if (!Gyro_block.IsFunctional) return;                // Ignore damaged or build progress blocks.
                if (Gyro_block.CubeGrid.Physics == null) return;     // Ignore ghost grids (projections).
				if (Gyro_block.IsWorking) newRotMatrixY = _rotMatrixY;
                else newRotMatrixY = Matrix.Identity;
                if (InitSubpart)
                {
                    Gyro_block.TryGetSubpart("AnimGyroInside", out GyroSubpart);
                    InitSubpart = false;
                }

                // Checks if subpart is removed (i.e. when changing block color).
                if (GyroSubpart.Closed.Equals(true)) ResetLostSubpart();

                if (GyroSubpart != null)
                {
                    var hingePos = new Vector3(HingePosX, HingePosY, HingePosZ); // This defines the location of a new pivot point.
                    var MatrixTransl1 = Matrix.CreateTranslation(-(hingePos));
                    var MatrixTransl2 = Matrix.CreateTranslation(hingePos);
                    var rotMatrix = GyroSubpart.PositionComp.LocalMatrix;
                    // rotMatrix *= (MatrixTransl1 * (Matrix.CreateRotationX(RotX) * Matrix.CreateRotationY(RotY) * Matrix.CreateRotationZ(RotZ)) * MatrixTransl2);
                    rotMatrix *= (MatrixTransl1 * newRotMatrixY * _rotMatrixX * _rotMatrixZ * MatrixTransl2);
                    GyroSubpart.PositionComp.LocalMatrix = rotMatrix;
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
            MyEntitySubpart Gyrosubpart;
            if (Gyro_block != null  && Gyro_block.IsWorking && Gyro_block.IsFunctional && Gyro_block.TryGetSubpart("AnimGyroInside", out Gyrosubpart))
            {
                var _emcolor = Gyro_block.GyroPower;
                Gyrosubpart.SetEmissiveParts("Emissive", Color.Green, _emcolor);
            }
            else if (Gyro_block != null && Gyro_block.TryGetSubpart("AnimGyroInside", out Gyrosubpart))
            {
                var _emoff = Gyro_block.GyroPower;
                Gyrosubpart.SetEmissiveParts("Emissive", Color.Red, _emoff);
            }
        }

            private void ResetLostSubpart()
        {
            GyroSubpart.Subparts.Clear();
            Gyro_block.TryGetSubpart("AnimGyroInside", out GyroSubpart);
        }
    }
}