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

            [HideInInspector]
            public float currentLerp;

            [HideInInspector]
            public int startPoint = 0;
        }

        [SerializeField] private float itemSpacing = 1f;
        [SerializeField] private float speed = 2f;
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private List<ConveyorBeltItem> items;

        private void FixedUpdate()
        {
            if (lineRenderer.positionCount < 2)
                return;

            for (int i = 0; i < items.Count; i++)
            {
                ConveyorBeltItem beltItem = items[i];

                if (beltItem.item == null)
                    continue;

                // Giữ khoảng cách với item phía trước
                if (i > 0)
                {
                    float distanceToPrevious =
                        Vector3.Distance(
                            beltItem.item.position,
                            items[i - 1].item.position);

                    if (distanceToPrevious <= itemSpacing)
                        continue;
                }

                int currentPoint = beltItem.startPoint;
                int nextPoint = (currentPoint + 1) % lineRenderer.positionCount;

                Vector3 startPos = lineRenderer.GetPosition(currentPoint);
                Vector3 endPos = lineRenderer.GetPosition(nextPoint);

                beltItem.item.position = Vector3.Lerp(
                    startPos,
                    endPos,
                    beltItem.currentLerp);

                float segmentLength = Vector3.Distance(startPos, endPos);

                if (segmentLength > 0.0001f)
                {
                    beltItem.currentLerp +=
                        (speed * Time.fixedDeltaTime) / segmentLength;
                }

                while (beltItem.currentLerp >= 1f)
                {
                    beltItem.currentLerp -= 1f;

                    beltItem.startPoint =
                        (beltItem.startPoint + 1) %
                        lineRenderer.positionCount;
                }
            }
        }
    }
}