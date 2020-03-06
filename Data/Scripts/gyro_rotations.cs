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
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Gyro), false, new string[] { "LBAnimGyro", "LBAnimGyroInside", "SBAnimGyro" })] // Here you can add other Gyro SubtypeIds.
    public class RotatingGyroLarge : MyGameLogicComponent
    {
        private IMyGyro Gyro_block;
        private MyEntitySubpart GyroSubpart;
        private string SubpartName;
        private float HingePosX;
        private float HingePosY;
        private float HingePosZ;
        private bool InitSubpart = true;
        private Matrix _rotMatrixX;
        private Matrix _rotMatrixY;
        private Matrix _rotMatrixZ;
        private Vector3 HingePos;
        private Matrix MatrixTransl1;
        private Matrix MatrixTransl2;
        private bool EmissivesSetToGreen;
        private bool EmissivesSetToRed;
        private float RotationSpeed = 0.02f; // Base rotation speed which will be used when creating subpart rotations.

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
            Gyro_block = (IMyGyro)Entity;
            _rotMatrixX = Matrix.Identity;
            _rotMatrixY = Matrix.Identity;
            _rotMatrixZ = Matrix.Identity;

            // Here you can add different hinge positions and subpart names for the Gyros you have added at the top.
            switch (Gyro_block.BlockDefinition.SubtypeId)
            {
                case "LBAnimGyro":
                    HingePosX = 0;
                    HingePosY = -0.051649f;
                    HingePosZ = -0.088645f;
                    SubpartName = "AnimGyro";
                    break;
                case "LBAnimGyroInside":
                    HingePosX = 0;
                    HingePosY = -0.051649f;
                    HingePosZ = -0.088645f;
                    SubpartName = "AnimGyroInside";
                    break;
                case "SBAnimGyro":
                    HingePosX = 0;
                    HingePosY = 0;
                    HingePosZ = 0;
                    SubpartName = "SAnimGyro";
                    break;
            }

            HingePos = new Vector3(HingePosX, HingePosY, HingePosZ);
            MatrixTransl1 = Matrix.CreateTranslation(-HingePos);
            MatrixTransl2 = Matrix.CreateTranslation(HingePos);
        }

        public override void Close() => NeedsUpdate = MyEntityUpdateEnum.NONE;

        public override void UpdateBeforeSimulation()
        {
            try
            {
                if (MyAPIGateway.Utilities.IsDedicated || Gyro_block.CubeGrid.Physics == null) return;
                if (GyroSubpart == null || GyroSubpart.Closed) GetSubpart();
                if (Gyro_block.IsFunctional && Gyro_block.IsWorking)
                {
                    if (!EmissivesSetToGreen)
                    {
                        GyroSubpart.SetEmissiveParts("Emissive", Color.Green, Gyro_block.GyroPower);
                        EmissivesSetToGreen = true;
                    }

                    bool camIsCloseToGyro = Vector3D.DistanceSquared(Gyro_block.WorldMatrix.Translation, MyAPIGateway.Session.Camera.Position) < 500;
                    float gridRotation = Gyro_block.CubeGrid.Physics.AngularVelocity.AbsMax();
                    if (camIsCloseToGyro && gridRotation > 0)
                    {
                        // The following two variables determine how the subpart will behave when the ship is turning.
                        // They have the same name and only one should be used at a time. Comment the other one so the script doesn't break.
                        float rotationSpeedModifier = Gyro_block.CubeGrid.Physics.AngularVelocity.Sum;              // This will rotate the subpart in both directions depending on how the ship is turning.
                        //float rotationSpeedModifier = Gyro_block.CubeGrid.Physics.AngularVelocity.Normalize();    // This will always rotate the subpart in one direction.

                        // Here you can choose on which axis the subpart will rotate and with what speed based on the SubtypeId.
                        // You can see how the X and Y axis are handled as well as how to increase the rotation speed by 10. 
                        switch (Gyro_block.BlockDefinition.SubtypeId)
                        {
                            case "LBAnimGyro":
                                _rotMatrixX = Matrix.CreateRotationX(RotationSpeed * rotationSpeedModifier);
                                break;
                            case "LBAnimGyroInside":
                                _rotMatrixY = Matrix.CreateRotationY(RotationSpeed * rotationSpeedModifier * 10);
                                break;
                            case "SBAnimGyro":
                                _rotMatrixX = Matrix.CreateRotationX(RotationSpeed * rotationSpeedModifier);
                                break;
                        }

                        Matrix rotMatrix = GyroSubpart.PositionComp.LocalMatrix;
                        rotMatrix *= MatrixTransl1 * _rotMatrixX * _rotMatrixY * _rotMatrixZ * MatrixTransl2;
                        GyroSubpart.PositionComp.SetLocalMatrix = rotMatrix;
                    }
                }
                else
                {
                    if (!EmissivesSetToRed)
                    {
                        GyroSubpart.SetEmissiveParts("Emissive", Color.Red, Gyro_block.GyroPower);
                        EmissivesSetToRed = true;
                    }
                }
            }
            catch (Exception e)
            {
                // This is for your benefit. Remove or change with your logging option before publishing.
                MyAPIGateway.Utilities.ShowNotification("Error: " + e.Message, 16);
            }
        }

        private void GetSubpart()
        {
            if (!InitSubpart) GyroSubpart.Subparts.Clear();
            Gyro_block.TryGetSubpart(SubpartName, out GyroSubpart);
            InitSubpart = false;
        }
    }
}