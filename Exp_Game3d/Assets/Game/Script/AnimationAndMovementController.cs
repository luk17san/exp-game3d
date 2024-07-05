/*using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Platformer
{
    public class AnimationAndMovementController : MonoBehaviour
    {
        CharacterController _characterController;
        Animator _animator;
        PlayerInput _playerInput;

        // variable to store 
        int _isWalkingHash;
        int _isRunningHash;

        //variable to store player input values

        Vector2 _currentMovementInput;
        Vector3 _currentMovement;
        Vector3 _currentRunMovement;
        Vector3 _appliedMovement;
        bool _isMovementPressed;
        bool _isRunPressed;

        // constans
        float _rotationFactorPerFrame = 15.0f;
        float _runMultiplier = 4.0f;
        int _zero = 0;

        //gravity variables
        float _gravity = -9.8f;
        float _groundedGravity = -.05f;

        //jumping variables
        bool _isJumpPressed = false;
        float _initialJumpVelocity;
        float _maxJumpHeight = 4.0f;
        float _maxJumpTime = 0.75f;
        bool _isJumping = false;
        int _isJumpingHash;
        int _jumpCountHash;
        bool _isJumpAnimating = false;
        int _jumpCount = 0;
        Dictionary<int, float> _initialJumpVelocities = new Dictionary<int, float>();
        Dictionary<int, float> _jumpGravities = new Dictionary<int, float>();
        Coroutine _currentJumpResetRoutine = null;
        private void Awake()
        {
            _playerInput = new PlayerInput();
            _characterController = GetComponent<CharacterController>();
            _animator = GetComponent<Animator>();

            _isWalkingHash = Animator.StringToHash("isWalking");
            _isRunningHash = Animator.StringToHash("isRunning");
            _isJumpingHash = Animator.StringToHash("isJumping");
            _jumpCountHash = Animator.StringToHash("jumpCount");

            //set the player input callback
            _playerInput.CharacterControls.Move.started += onMovementInput;
            _playerInput.CharacterControls.Move.canceled += onMovementInput;
            _playerInput.CharacterControls.Move.performed += onMovementInput;
            _playerInput.CharacterControls.Run.started += onRun ;
            _playerInput.CharacterControls.Run.canceled += onRun;
            _playerInput.CharacterControls.Jump.started += onJump;
            _playerInput.CharacterControls.Jump.canceled += onJump;

            setupJumpVariables();
        }

         void setupJumpVariables()
        {
            float timeToApex = _maxJumpTime / 2;
            _gravity = (-2 * _maxJumpHeight) / Mathf.Pow(timeToApex, 2);
            _initialJumpVelocity = (2 * _maxJumpHeight) / timeToApex;

            float secondJumpGravity = (-2 * (_maxJumpHeight + 2)) / Mathf.Pow((timeToApex * 1.25f), 2);
            float secondJumpInotialVelocity = (2 * (_maxJumpHeight + 2)) / (timeToApex * 1.25f);
            float thirdJumpGravity = (-2 * (_maxJumpHeight + 4)) / Mathf.Pow((timeToApex * 1.5f), 2);
            float thirdJumpInotialVelocity = (2 * (_maxJumpHeight + 4)) / (timeToApex * 1.5f);

            _initialJumpVelocities.Add(1, _initialJumpVelocity);
            _initialJumpVelocities.Add(2, secondJumpInotialVelocity);
            _initialJumpVelocities.Add(3, thirdJumpInotialVelocity);

            _jumpGravities.Add(0, _gravity);
            _jumpGravities.Add(1, _gravity);
            _jumpGravities.Add(2, secondJumpGravity);
            _jumpGravities.Add(3, thirdJumpGravity);

        }

        void handleJump()
        {
            if (!_isJumping && _characterController.isGrounded && _isJumpPressed) 
            {
                if (_jumpCount < 3 && _currentJumpResetRoutine != null)
                {
                    StopCoroutine(_currentJumpResetRoutine);
                }
                _animator.SetBool(_isJumpingHash, true);
                _isJumpAnimating = true;
                _isJumping = true;
                _jumpCount += 1;
                _animator.SetInteger(_jumpCountHash, _jumpCount);
                _currentMovement.y = _initialJumpVelocities[_jumpCount] * .5f;
                _appliedMovement.y = _initialJumpVelocities[_jumpCount] * .5f;
            } else if (!_isJumpPressed && _isJumping && _characterController.isGrounded)
            {
                _isJumping = false;
            }

            

        }
        IEnumerator jumpResetRoutine()
            {
                yield return new WaitForSeconds(.5f);
                _jumpCount = 0;
            }
        void handleRotation()
        {
            Vector3 positionToLookAt;
            //the change in position our character should point to
            positionToLookAt.x = _currentMovement.x;
            positionToLookAt.y = 0.0f;
            positionToLookAt.z = _currentMovement.z;
            //the current rotation of our character
            Quaternion currentRotation = transform.rotation;

            if (_isMovementPressed)
            {
                //creates a new rotation based on where the player is currently pressing
                Quaternion targetRotation = Quaternion.LookRotation(positionToLookAt);
                transform.rotation = Quaternion.Slerp(currentRotation, targetRotation, _rotationFactorPerFrame * Time.deltaTime);
            }
        }
        void onJump(InputAction.CallbackContext context)
        {
            _isJumpPressed = context.ReadValueAsButton();
        }

        void onRun(InputAction.CallbackContext context)
        {
            _isRunPressed = context.ReadValueAsButton();    
        }

        //handler funcion to set player input values
        void onMovementInput (InputAction.CallbackContext context)
        {
            _currentMovementInput = context.ReadValue<Vector2>();
            _currentMovement.x = _currentMovementInput.x;
            _currentMovement.z = _currentMovementInput.y;
            _currentRunMovement.x = _currentMovementInput.x * _runMultiplier;
            _currentRunMovement.z = _currentMovementInput.y * _runMultiplier;
            _isMovementPressed = _currentMovementInput.x != _zero || _currentMovementInput.y != _zero;
        }
        
        void handleAnimation()
        {
            bool isWalking = _animator.GetBool(_isWalkingHash);
            bool isRunning = _animator.GetBool(_isRunningHash);

            if(_isMovementPressed && !isWalking) 
            {
                _animator.SetBool(_isWalkingHash, true);
            }

            else if (!_isMovementPressed && isWalking) 
            {
            _animator.SetBool(_isWalkingHash, false);
            }

            if ((_isMovementPressed && _isRunPressed) && !isRunning) 
            {
                _animator.SetBool(_isRunningHash,true);
            }
            else if ((!_isMovementPressed || !_isRunPressed) && isRunning)
            {
                _animator.SetBool(_isRunningHash, false);
            }
        }

        void handleGravity ()
        {
            bool isFalling = _currentMovement.y <= 0.0f || !_isJumpPressed;
            float fallMultiplier = 2.0f;

            //apply proper gravity if the player is grounded or not
            if (_characterController.isGrounded)
            {
                if (_isJumpAnimating)
                {
                    _animator.SetBool(_isJumpingHash, false);
                    _isJumpAnimating = false;
                    _currentJumpResetRoutine = StartCoroutine(jumpResetRoutine());
                    if (_jumpCount == 3)
                    {
                        _jumpCount = 0;
                        _animator.SetInteger(_jumpCountHash, _jumpCount);
                    }
                }
                _currentMovement.y = _groundedGravity;
                _appliedMovement.y = _groundedGravity;
            }
            else if (isFalling)
            {

                float priviousYVelocity = _currentMovement.y;
                _currentMovement.y = _currentMovement.y + (_jumpGravities[_jumpCount] *fallMultiplier * Time.deltaTime);
                _appliedMovement.y = Mathf.Max((priviousYVelocity + _currentMovement.y) * .5f,-20.0f);
                
            }
            else
            {
                float priviousYVelocity = _currentMovement.y;
                _currentMovement.y = _currentMovement.y + (_jumpGravities[_jumpCount] * Time.deltaTime);
                _appliedMovement.y = (priviousYVelocity + _currentMovement.y) * .5f;
                
            }
        }

        // Update is called once per frame
        void Update()
        {
            handleRotation();
            handleAnimation();

            if (_isRunPressed)
            {
                _appliedMovement.x = _currentRunMovement.x;
                _appliedMovement.z = _currentRunMovement.z;
            }
            else 
            {
                _appliedMovement.x = _currentMovement.x;
                _appliedMovement.z = _currentMovement.z;
            }

            _characterController.Move(_appliedMovement * Time.deltaTime);

            handleGravity();
            handleJump();
        }

         void OnEnable()
        {
            _playerInput.CharacterControls.Enable();
        }
         void OnDisable()
        {
            _playerInput.CharacterControls.Disable();
        }
    }
}
*/  