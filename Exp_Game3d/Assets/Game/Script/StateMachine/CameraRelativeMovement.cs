using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Platformer
{
    public class CameraRelativeMovement : MonoBehaviour
    {
        float _horizontalInput;
        float _verticalInput;
        Vector3 _playerInput;
        [SerializeField] CharacterController _characterController;

        void Start()
        {
        
            _characterController = GetComponent<CharacterController>();
        }

        // Update is called once per frame
        void Update()
        {
            // get and store player input every frame
            _horizontalInput = Input.GetAxis("Horizontal");
            _verticalInput = Input.GetAxis("Vertical");

            // set it to the X and Z values of the Vector3
            _playerInput.x = _horizontalInput;
            _playerInput.y = _verticalInput;

            // rotate player input vector to camera space
            Vector3 cameraRelativeMovement = ConvertToCameraSpace(_playerInput);
            //transform position using Move and the rotated player input
            _characterController.Move(cameraRelativeMovement * Time.deltaTime);

        }

        Vector3 ConvertToCameraSpace(Vector3 vectoreToRotate)
        {
            //get the forward and right directional vectors of the camera
            Vector3 cameraForward = Camera.main.transform.forward;
            Vector3 cameraRight = Camera.main.transform.right;

            //remove the Y values to ignore upward/downward camera angles
            cameraForward.y = 0;
            cameraRight.y = 0;

            //re-normalize both vectors so they each have a magnitude of 1 
            cameraForward = cameraForward.normalized;
            cameraRight = cameraRight.normalized;

            //rotat the X and Z VectorToRotate values to camera space
            Vector3 cameraFormardZProduct = vectoreToRotate.z * cameraForward;
            Vector3 cameraRightXProduct = vectoreToRotate.x * cameraRight;

            //the sum of the both product is the Vectore3 in camera space
            Vector3 vectorRotateToCameraSpace = cameraFormardZProduct + cameraRightXProduct;
            return vectorRotateToCameraSpace;
        }
    }
}
