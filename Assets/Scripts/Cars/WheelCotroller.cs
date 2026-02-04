using UnityEngine;
//using system;

public class WheelCotroller : MonoBehaviour
{
   [SerializeField] private Transform МодельКолеса;

   [SerializeField] private WheelCollider  КоллайдерКолеса;

public WheelCollider WheelCollider
    {
        get
        {
            return КоллайдерКолеса;
        }
    }
    
    public bool ПередниеКолеса;
    
        private Vector3 position;
        private Quaternion rotation;   
    
    private void Update()
    {
        WheelCollider.GetWorldPose(out position, out rotation);
        МодельКолеса.transform.position = position;
        МодельКолеса.transform.rotation = rotation;
    }
}
