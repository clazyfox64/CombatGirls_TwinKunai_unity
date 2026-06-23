using UnityEngine;

namespace CombatGirlsCharacterPack
{
    [ExecuteAlways]
    public class SDFFaceShadowController : MonoBehaviour
    {
        public enum BoneAxis
        {
            Forward,
            Backward,
            Right,
            Left,
            Up,
            Down
        }

        [Header("References")]
        public Transform headBone;
        public Renderer[] faceRenderers;

        [Header("Bone Orientation")]
        public BoneAxis headForwardAxis = BoneAxis.Forward;
        public BoneAxis headRightAxis = BoneAxis.Right;

        [Header("Shader Properties")]
        public bool forceSDFShadow = true;
        public bool enableSDFShadow = true;
        public bool forceScriptVectors = true;
        public string useSDFShadowProp = "_UseSDFShadow";
        public string useScriptVectorsProp = "_UseScriptVectors";
        public string faceForwardProp = "_FaceForward";
        public string faceRightProp = "_FaceRight";

        private MaterialPropertyBlock propBlock;

        private void OnEnable()
        {
            Apply();
        }

        private void LateUpdate()
        {
            Apply();
        }

        private void OnValidate()
        {
            Apply();
        }

        private void Apply()
        {
            if (headBone == null || !HasAnyRenderer())
            {
                return;
            }

            Vector3 forward = SafeNormalize(GetAxisVector(headBone, headForwardAxis), transform.forward);
            Vector3 right = SafeNormalize(GetAxisVector(headBone, headRightAxis), transform.right);

            if (faceRenderers == null)
            {
                return;
            }

            for (int i = 0; i < faceRenderers.Length; i++)
            {
                ApplyToRenderer(faceRenderers[i], forward, right);
            }
        }

        private bool HasAnyRenderer()
        {
            if (faceRenderers == null)
            {
                return false;
            }

            for (int i = 0; i < faceRenderers.Length; i++)
            {
                if (faceRenderers[i] != null)
                {
                    return true;
                }
            }

            return false;
        }

        private void ApplyToRenderer(Renderer targetRenderer, Vector3 forward, Vector3 right)
        {
            if (targetRenderer == null)
            {
                return;
            }

            if (propBlock == null)
            {
                propBlock = new MaterialPropertyBlock();
            }

            targetRenderer.GetPropertyBlock(propBlock);
            WriteSDFProperties(propBlock, targetRenderer, forward, right);
            targetRenderer.SetPropertyBlock(propBlock);

            Material[] sharedMaterials = targetRenderer.sharedMaterials;
            for (int i = 0; i < sharedMaterials.Length; i++)
            {
                targetRenderer.GetPropertyBlock(propBlock, i);
                WriteSDFProperties(propBlock, targetRenderer, forward, right);
                targetRenderer.SetPropertyBlock(propBlock, i);
            }
        }

        private void WriteSDFProperties(MaterialPropertyBlock block, Renderer targetRenderer, Vector3 forward, Vector3 right)
        {
            if (forceSDFShadow)
            {
                block.SetFloat(useSDFShadowProp, enableSDFShadow ? 1.0f : 0.0f);
            }
            else
            {
                block.SetFloat(useSDFShadowProp, GetMaterialFloat(targetRenderer, useSDFShadowProp, 0.0f));
            }

            if (forceScriptVectors)
            {
                block.SetFloat(useScriptVectorsProp, 1.0f);
            }
            else
            {
                block.SetFloat(useScriptVectorsProp, GetMaterialFloat(targetRenderer, useScriptVectorsProp, 0.0f));
            }
            block.SetVector(faceForwardProp, new Vector4(forward.x, forward.y, forward.z, 0.0f));
            block.SetVector(faceRightProp, new Vector4(right.x, right.y, right.z, 0.0f));
        }

        private static float GetMaterialFloat(Renderer targetRenderer, string propertyName, float fallback)
        {
            Material sharedMaterial = targetRenderer.sharedMaterial;
            if (sharedMaterial == null || !sharedMaterial.HasProperty(propertyName))
            {
                return fallback;
            }

            return sharedMaterial.GetFloat(propertyName);
        }

        private static Vector3 SafeNormalize(Vector3 value, Vector3 fallback)
        {
            return value.sqrMagnitude > 0.000001f ? value.normalized : fallback.normalized;
        }

        private static Vector3 GetAxisVector(Transform target, BoneAxis axis)
        {
            switch (axis)
            {
                case BoneAxis.Forward:
                    return target.forward;
                case BoneAxis.Backward:
                    return -target.forward;
                case BoneAxis.Right:
                    return target.right;
                case BoneAxis.Left:
                    return -target.right;
                case BoneAxis.Up:
                    return target.up;
                case BoneAxis.Down:
                    return -target.up;
                default:
                    return target.forward;
            }
        }
    }
}
