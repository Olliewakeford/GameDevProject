using UnityEngine;

public class PhysicsMaterialCreator : MonoBehaviour
{
    [ContextMenu("Create Ball Physics Material")]
    public void CreateBallPhysicsMaterial()
    {
        // Create physics material
        PhysicMaterial ballPhysics = new PhysicMaterial("BallPhysics");
        
        // Set properties
        ballPhysics.dynamicFriction = 0.3f;
        ballPhysics.staticFriction = 0.2f;
        ballPhysics.bounciness = 0.3f;
        ballPhysics.frictionCombine = PhysicMaterialCombine.Average;
        ballPhysics.bounceCombine = PhysicMaterialCombine.Average;
        
        // Save asset
#if UNITY_EDITOR
        if (!System.IO.Directory.Exists("Assets/Materials"))
        {
            System.IO.Directory.CreateDirectory("Assets/Materials");
        }
        
        UnityEditor.AssetDatabase.CreateAsset(ballPhysics, "Assets/Materials/BallPhysics.physicMaterial");
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();
        
        Debug.Log("Created BallPhysics material at Assets/Materials/BallPhysics.physicMaterial");
#endif
    }
}