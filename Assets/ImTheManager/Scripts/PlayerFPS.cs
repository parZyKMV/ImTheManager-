using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class RPS_ThirdPersonController : MonoBehaviour
{
    // ===== INSPECTOR =====================================================

    [Header("Movimiento")]
    [SerializeField] private float walkSpeed = 3f;   // velocidad base en m/s
    [SerializeField] private float sprintMultiplier = 1.7f; // multiplicador al correr
    [SerializeField] private float rotationSmoothTime = 0.1f; // suavizado de rotacion

    [Header("Salto y gravedad")]
    [SerializeField] private float gravity = -20f; // gravedad en m/s^2
    [SerializeField] private float jumpForce = 6f;  // impulso inicial del salto
    [SerializeField] private float groundCheckDistance = 0.3f; // distancia del Raycast al suelo

    [Header("ZVelocity - blend tree del salto")]
    // Que tan rapido ZVelocity sigue a la velocidad vertical real.
    // Valor alto = transicion instantanea, valor bajo = suave y gradual.
    [SerializeField] private float zVelocitySmoothing = 8f;

    [Header("Referencias")]
    [SerializeField] private Transform cameraTransform; // arrastra Main Camera aqui

    [Header("Particulas de pasos (polvo/humo)")]
    [SerializeField] public ParticleSystem footstepParticles; // arrastra el ParticleSystem aqui
    [SerializeField] private float walkEmissionRate = 8f;   // particulas por segundo al caminar
    [SerializeField] private float sprintEmissionRate = 20f; // particulas por segundo al correr
    [SerializeField] private float emissionSmoothing = 10f; // que tan rapido sube/baja la emision

    // ===== PRIVADOS =======================================================

    private CharacterController _controller;
    private Animator _animator;

    // Sistemas futuros (stats, stamina, combate) - se conectan mas adelante.
    // Por ahora el controller funciona 100% standalone con walkSpeed fijo.
    //private StatSystem _statSystem;
    //private StaminaSystem _staminaSystem;
    //private PlayerCombatController _combatController;

    private Vector3 _velocity;          // velocidad acumulada (salto + gravedad)
    private float _turnSmoothVelocity;  // referencia interna para SmoothDampAngle
    private bool _isGrounded;           // true si los pies tocan el suelo

    // ZVelocity suavizada - lo que realmente mandamos al Animator.
    // No mandamos _velocity.y directo porque da saltos bruscos en el blend tree.
    private float _smoothedZVelocity = 0f;

    // Estado de movimiento del frame actual, usado por las particulas de pasos.
    private bool _isMoving = false;
    private bool _isSprinting = false;

    // Emision actual (suavizada) del ParticleSystem de polvo.
    private float _currentEmissionRate = 0f;
    private ParticleSystem.EmissionModule _footstepEmission;

    // ===== INPUT ACTIONS ===================================================

    private InputAction _moveAction;
    private InputAction _jumpAction;
    private InputAction _sprintAction;

    // ===== AWAKE ============================================================

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _animator = GetComponentInChildren<Animator>();

        // Busca las acciones en el Input Actions asset (Project-wide Actions)
        var actions = InputSystem.actions;

        if (actions == null)
        {
            Debug.LogError("[RPS_ThirdPersonController] No se encontro un Input Actions asset asignado como Project-wide Actions.");
            return;
        }

        _moveAction = actions.FindAction("Move");
        _jumpAction = actions.FindAction("Jump");
        _sprintAction = actions.FindAction("Sprint");

        if (_moveAction == null) Debug.LogWarning("[RPS_ThirdPersonController] No se encontro la accion 'Move'.");
        if (_jumpAction == null) Debug.LogWarning("[RPS_ThirdPersonController] No se encontro la accion 'Jump'.");
        if (_sprintAction == null) Debug.LogWarning("[RPS_ThirdPersonController] No se encontro la accion 'Sprint'.");

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform; // fallback automatico

        if (footstepParticles != null)
        {
            _footstepEmission = footstepParticles.emission;
            _footstepEmission.rateOverTime = 0f; // arranca apagado
        }
    }

    void OnEnable()
    {
        _moveAction?.Enable();
        _jumpAction?.Enable();
        _sprintAction?.Enable();
    }

    void OnDisable()
    {
        _moveAction?.Disable();
        _jumpAction?.Disable();
        _sprintAction?.Disable();
    }

    // ===== UPDATE ============================================================

    void Update()
    {
        HandleGround();
        HandleMovement();
        HandleJump();
        ApplyGravity();
        UpdateAnimator();
        HandleFootstepParticles();
    }

    // ===== SUELO ==============================================================

    void HandleGround()
    {
        // Raycast es mas confiable que controller.isGrounded.
        // Sube el origen 10cm para evitar que el rayo empiece dentro del collider.
        _isGrounded = Physics.Raycast(
            transform.position + Vector3.up * 0.1f,
            Vector3.down,
            _controller.height / 2f + groundCheckDistance
        );

        // Resetea la velocidad vertical al tocar suelo.
        // -2 en lugar de 0 para que el Raycast siga detectando suelo
        // en el siguiente frame sin perder el contacto.
        if (_isGrounded && _velocity.y < 0f)
            _velocity.y = -2f;
    }

    // ===== MOVIMIENTO ==========================================================

    void HandleMovement()
    {
        if (_moveAction == null) return;

        Vector2 input = _moveAction.ReadValue<Vector2>();
        Vector3 direction = new Vector3(input.x, 0f, input.y).normalized;

        // Por ahora la velocidad es fija (walkSpeed). Cuando agregues StatSystem
        // reemplaza esta linea por: _statSystem != null ? _statSystem.MoveSpeed : walkSpeed
        float currentSpeed = walkSpeed;

        // Sprint simple sin stamina por ahora.
        bool isSprinting = _sprintAction != null && _sprintAction.IsPressed();

        if (isSprinting)
            currentSpeed *= sprintMultiplier;

        // Guardamos el estado para HandleFootstepParticles() y para no
        // recalcular direction.magnitude fuera de este metodo.
        _isMoving = direction.magnitude >= 0.1f;
        _isSprinting = isSprinting;

        if (_isMoving && cameraTransform != null)
        {
            // Calcula el angulo de rotacion relativo a la camara.
            float targetAngle = Mathf.Atan2(direction.x, direction.z)
                              * Mathf.Rad2Deg
                              + cameraTransform.eulerAngles.y;

            float angle = Mathf.SmoothDampAngle(
                transform.eulerAngles.y,
                targetAngle,
                ref _turnSmoothVelocity,
                rotationSmoothTime
            );

            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            _controller.Move(moveDir.normalized * currentSpeed * Time.deltaTime);
        }

        // Manda la velocidad real al Animator para el blend tree de movimiento.
        if (_animator != null)
            _animator.SetFloat("Speed", direction.magnitude * currentSpeed);
    }

    // ===== SALTO ================================================================

    void HandleJump()
    {
        if (_jumpAction == null) return;

        // WasPressedThisFrame = true solo el frame exacto del input.
        if (_jumpAction.WasPressedThisFrame() && _isGrounded)
        {
            _velocity.y = jumpForce;

            if (_animator != null)
                _animator.SetTrigger("Jump");
        }
    }

    // ===== PARTICULAS DE PASOS =====================================================

    void HandleFootstepParticles()
    {
        if (footstepParticles == null) return;

        // Solo emitimos polvo si esta en el suelo y moviendose.
        // En el aire o quieto la emision baja a 0 (no se detiene el sistema,
        // solo deja de generar particulas nuevas, para que las existentes
        // terminen su ciclo de vida de forma natural).
        float targetRate = 0f;

        if (_isGrounded && _isMoving)
            targetRate = _isSprinting ? sprintEmissionRate : walkEmissionRate;

        // Suavizamos el cambio de emision para evitar cortes bruscos
        // al empezar/dejar de correr.
        _currentEmissionRate = Mathf.Lerp(
            _currentEmissionRate,
            targetRate,
            emissionSmoothing * Time.deltaTime
        );

        _footstepEmission.rateOverTime = _currentEmissionRate;

        // Play/Stop del sistema: lo activamos cuando hay algo que emitir
        // y lo detenemos (sin limpiar particulas activas) cuando no.
        //Debug.Log($"FootstepParticles: targetRate={targetRate}, currentEmissionRate={_currentEmissionRate}, isPlaying={footstepParticles.isPlaying}");
        if (targetRate > 0f && !footstepParticles.isPlaying)
            footstepParticles.Play();
        else if (targetRate <= 0f && _currentEmissionRate < 0.05f && footstepParticles.isPlaying)
            footstepParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    // ===== GRAVEDAD ==============================================================

    void ApplyGravity()
    {
        _velocity.y += gravity * Time.deltaTime;
        _controller.Move(Vector3.up * _velocity.y * Time.deltaTime);
    }

    // ===== ANIMATOR ===============================================================

    void UpdateAnimator()
    {
        if (_animator == null) return;

        // ZVelocity para el blend tree del salto:
        // En lugar de mandar _velocity.y directo (que puede ser -40 o mas),
        // lo normalizamos entre -1 y 1 y lo suavizamos.
        float targetZVelocity;

        if (_isGrounded)
        {
            targetZVelocity = 0f;
        }
        else
        {
            // _velocity.y positivo (subiendo) -> ZVelocity negativo -> Jump_Start
            // _velocity.y negativo (bajando)  -> ZVelocity positivo -> Jump_Land
            targetZVelocity = Mathf.Clamp(-_velocity.y / jumpForce, -1f, 1f);
        }

        _smoothedZVelocity = Mathf.Lerp(
            _smoothedZVelocity,
            targetZVelocity,
            zVelocitySmoothing * Time.deltaTime
        );

        _animator.SetFloat("ZVelocity", _smoothedZVelocity);
        _animator.SetBool("IsGrounded", _isGrounded);
    }

    // ===== CURSOR ==================================================================

    /// <summary>
    /// Bloquea el cursor. Llamar al cerrar cualquier menu.
    /// </summary>
    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /// <summary>
    /// Libera el cursor. Llamar al abrir la UI de seleccion de arma.
    /// </summary>
    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // ===== GIZMOS ===================================================================

    void OnDrawGizmos()
    {
        // Verde = detecta suelo, Rojo = no detecta suelo.
        if (_controller == null) return;

        Gizmos.color = _isGrounded ? Color.green : Color.red;
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        Gizmos.DrawLine(
            origin,
            origin + Vector3.down * (_controller.height / 2f + groundCheckDistance)
        );
    }
}