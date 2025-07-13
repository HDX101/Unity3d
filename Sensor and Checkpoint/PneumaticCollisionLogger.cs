using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PneumaticCollisionLogger : MonoBehaviour
{
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("PushableObject"))
        {
            var objectData = collision.gameObject.GetComponent<SortableObjectData>();
            if (objectData != null && !objectData.PneumaticSorted_Unity)
            {
                objectData.PneumaticSorted_Unity = true;
            }
        }
    }
}