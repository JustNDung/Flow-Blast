using System.Collections.Generic;
using UnityEngine;

namespace ConveyorBelt
{
    public class ConveyorBelt : MonoBehaviour
    {
        [System.Serializable]
        public class ConveyorBeltItem
        {
            public Transform item;
            [HideInInspector] public float currentLerp;
            [HideInInspector] public int startPoint = 1;
        }

        [SerializeField] private float itemSpacing;
        [SerializeField] private float speed;
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private List<ConveyorBeltItem> items;

        private void FixedUpdate()
        {
            for (int i = 0; i < items.Count; i++)
            {
                ConveyorBeltItem beltItem = items[i];
                Transform item = items[i].item;

                if (i > 0)
                {
                    if (Vector3.Distance(item.position, items[i - 1].item.position) <= itemSpacing)
                    {
                        continue;
                    }
                }
                
                item.transform.position = Vector3.Lerp(lineRenderer.GetPosition(beltItem.startPoint), lineRenderer.GetPosition(beltItem.startPoint + 1), beltItem.currentLerp);
                float distance = Vector3.Distance(lineRenderer.GetPosition(beltItem.startPoint), lineRenderer.GetPosition(beltItem.startPoint + 1));
                beltItem.currentLerp += speed * Time.deltaTime / distance;

                if (beltItem.currentLerp >= 1)
                {
                    if (beltItem.startPoint + 2 < lineRenderer.positionCount)
                    {
                        beltItem.currentLerp = 0;
                        beltItem.startPoint++;
                    }
                    else
                    {
                    
                    }
                }
            }
        }
    }
}
