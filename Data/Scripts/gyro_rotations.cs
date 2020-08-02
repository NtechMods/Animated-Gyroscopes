using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using System;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace nukeguardRotatingGyrosNoVanilla
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Gyro), false, new string[] { "LBAnimGyro", "LBAnimGyroInside", "SBAnimGyro" })] // Here you can add other Gyro SubtypeIds.
    public class RotatingGyroLarge : MyGameLogicComponent
    {
        private IMyGyro Gyro_block;
        private MyEntitySubpart GyroSubpart;
        private string SubpartName;
        private Matrix Rotate_MatrixX;
        private Matrix Rotate_MatrixY;
        private Matrix Rotate_MatrixZ;
        private Matrix Matrix_Translate1;
        private Matrix Matrix_Translate2;
        private bool EmissivesSetToGreen;
        private bool EmissivesSetToYellow;
        private bool EmissivesSetToRed;
        private float RotationSpeed = 0.02f; // Base rotation speed which will be used when creating subpart rotations.

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
            Gyro_block = (IMyGyro)Entity;
            Rotate_MatrixX = Matrix.Identity;
            Rotate_MatrixY = Matrix.Identity;
            Rotate_MatrixZ = Matrix.Identity;

            // Here you can add different hinge positions and subpart names for the Gyros you have added at the top.
            Vector3 hingePos;
            switch (Gyro_block.BlockDefinition.SubtypeId)
            {
                case "LargeBlockGyro":
                    hingePos = new Vector3(x: 0, y: -0.051649f, z: -0.088645f);
                    SubpartName = "AnimGyro";
                    break;
                case "LBAnimGyroInside":
                    hingePos = new Vector3(x: 0, y: -0.051649f, z: -0.088645f);
                    SubpartName = "AnimGyroInside";
                    break;
                case "SmallBlockGyro":
                    hingePos = new Vector3(x: 0, y: 0, z: 0);
                    SubpartName = "SAnimGyro";
                    break;
                default:
                    hingePos = Vector3.Zero;
                    break;
            }
            Matrix_Translate1 = Matrix.CreateTranslation(-hingePos);
            Matrix_Translate2 = Matrix.CreateTranslation(hingePos);
        }

        public override void Close() => NeedsUpdate = MyEntityUpdateEnum.NONE;

        public override void UpdateBeforeSimulation()
        {
            try
            {
                if (MyAPIGateway.Utilities.IsDedicated || Gyro_block.CubeGrid.Physics == null) return;
                if ((GyroSubpart == null || GyroSubpart.Closed) && !GetSubpart()) return;
                if (Gyro_block.IsFunctional && Gyro_block.IsWorking)
                {
                    if (Gyro_block.GyroOverride)
                    {
                        if (!EmissivesSetToYellow) SetGyroSubpartEmissives(Color.Yellow, setYellow: true);
                    }
                    else
                    {
                        if (!EmissivesSetToGreen) SetGyroSubpartEmissives(Color.Green, setGreen: true);
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
                            case "LargeBlockGyro":
                                Rotate_MatrixX = Matrix.CreateRotationX(RotationSpeed * rotationSpeedModifier);
                                break;
                            case "LBAnimGyroInside":
                                Rotate_MatrixY = Matrix.CreateRotationY(RotationSpeed * rotationSpeedModifier * 10);
                                break;
                            case "SmallBlockGyro":
                                Rotate_MatrixX = Matrix.CreateRotationX(RotationSpeed * rotationSpeedModifier);
                                break;
                        }

                        Matrix rotateMatrix = GyroSubpart.PositionComp.LocalMatrixRef;
                        rotateMatrix *= Matrix_Translate1 * Rotate_MatrixX * Rotate_MatrixY * Rotate_MatrixZ * Matrix_Translate2;
                        GyroSubpart.PositionComp.SetLocalMatrix(ref rotateMatrix, Gyro_block, true, ref rotateMatrix);
                    }
                }
                else
                {
                    if (!EmissivesSetToRed) SetGyroSubpartEmissives(Color.Red, setRed: true);
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"Error in: {e.Message}\n{e.StackTrace}");
            }
        }

        private void SetGyroSubpartEmissives(Color color, bool setGreen = false, bool setYellow = false, bool setRed = false)
        {
            GyroSubpart.SetEmissiveParts("Emissive", color, Gyro_block.GyroPower);
            EmissivesSetToGreen = setGreen;
            EmissivesSetToYellow = setYellow;
            EmissivesSetToRed = setRed;
        }

        private bool GetSubpart()
        {
            if (GyroSubpart != null) GyroSubpart.Subparts.Clear();
            return Gyro_block.TryGetSubpart(SubpartName, out GyroSubpart);
        }
    }
}