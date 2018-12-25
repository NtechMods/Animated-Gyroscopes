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
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Gyro), false, "SBAnimGyro")]
    public class RotatingGyroSmall : MyGameLogicComponent
    {
        public IMyGyro SGyro_block;
        public MyEntitySubpart SGyroSubpart;
        private float HingePosX = 0f; // Hinge position on the X axis. 0 is center.
        private float HingePosY = 0f; // Hinge position on the Y axis. 0 is center.
        private float HingePosZ = 0f; // Hinge position on the Z axis. 0 is center.
        private float RotX = 0.08f; // Rotation on the X axis. 0 is no rotation.
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
            SGyro_block = (IMyGyro)Entity;

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
                if (!SGyro_block.IsFunctional) return;                // Ignore damaged or build progress blocks.
                if (SGyro_block.CubeGrid.Physics == null) return;     // Ignore ghost grids (projections).
				if (SGyro_block.IsWorking) newRotMatrixX = _rotMatrixX;
                else newRotMatrixX = Matrix.Identity;
                if (InitSubpart)
                {
                    SGyro_block.TryGetSubpart("weight", out SGyroSubpart);
                    InitSubpart = false;
                }

                // Checks if subpart is removed (i.e. when changing block color).
                if (SGyroSubpart.Closed.Equals(true)) ResetLostSubpart();

                if (SGyroSubpart != null)
                {
                    var hingePos = new Vector3(HingePosX, HingePosY, HingePosZ); // This defines the location of a new pivot point.
                    var MatrixTransl1 = Matrix.CreateTranslation(-(hingePos));
                    var MatrixTransl2 = Matrix.CreateTranslation(hingePos);
                    var rotMatrix = SGyroSubpart.PositionComp.LocalMatrix;
                    // rotMatrix *= (MatrixTransl1 * (Matrix.CreateRotationX(RotX) * Matrix.CreateRotationY(RotY) * Matrix.CreateRotationZ(RotZ)) * MatrixTransl2);
                    rotMatrix *= (MatrixTransl1 * newRotMatrixX * _rotMatrixY * _rotMatrixZ * MatrixTransl2);
                    SGyroSubpart.PositionComp.LocalMatrix = rotMatrix;
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
            MyEntitySubpart SGyrosubpart;
            if (SGyro_block != null && SGyro_block.TryGetSubpart("weight", out SGyrosubpart) && SGyro_block.IsWorking && SGyro_block.IsFunctional)
            {
                var _emcolor = SGyro_block.GyroPower;
                SGyrosubpart.SetEmissiveParts("Emissive", Color.Green, _emcolor);
            }
            else if (SGyro_block != null && SGyro_block.TryGetSubpart("weight", out SGyrosubpart))
            {
                var _emoff = SGyro_block.GyroPower;
                SGyrosubpart.SetEmissiveParts("Emissive", Color.Red, _emoff);
            }
        }

            private void ResetLostSubpart()
        {
            SGyroSubpart.Subparts.Clear();
            SGyro_block.TryGetSubpart("weight", out SGyroSubpart);
        }
    }
}