using Fusion;
using UnityEngine;

public class Player : NetworkBehaviour
{
    private CharacterController _characterController;
    private NetworkCharacterController _networkCharacterController;

    private Vector3 _prevPos;

    private readonly float _runStateMass = 3.0f;
    private readonly float _idleStateMass = 1.0f;
    private readonly float _playerMoveLerpOffset = 15.0f;

    private float _mass = 1.0f;
    private float _moveSpeed = 5.0f;

    private bool _isMove = false;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _networkCharacterController = GetComponent<NetworkCharacterController>();
        _prevPos = transform.position;
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        if (GetInput(out PlayerInputData input))
        {
            // 점프 먼저 처리
            if (input.Jump)
            {
                _networkCharacterController.Jump();
            }

            _isMove = true;
            _mass = _runStateMass;

            Vector3 move = new Vector3(input.Horizontal, 0.0f, input.Vertical);
            move.Normalize();

            Vector3 desiredVelocity = move * _moveSpeed;
            _networkCharacterController.Velocity = Vector3.MoveTowards(
                _networkCharacterController.Velocity,
                desiredVelocity,
                _playerMoveLerpOffset * Runner.DeltaTime);

            // 충돌, 중력 포함 이동
            _networkCharacterController.Move(move * _moveSpeed * Runner.DeltaTime);
            
            // 이전 위치 저장
            _prevPos = transform.position;
        }
        else
        {
            _isMove = false;
            _mass = _idleStateMass;

            // 움직이지 않을 때 이전 위치로 텔레포트
            _networkCharacterController.Teleport(_prevPos);
        }

        // 땅에 닿으면 수직 속도 초기화, 수평 속도 감속 처리
        if (_networkCharacterController.Grounded)
        {
            Vector3 vel = _networkCharacterController.Velocity;
            vel.y = 0.0f;
            _networkCharacterController.Velocity = vel;
        }

        CheckCollision();
    }
   
    private void PushOtherPlayer(Player otherPlayer)
    {
        if (!_isMove) return;

        if (!otherPlayer.HasStateAuthority) return; // StateAuthority에서만 처리

        Vector3 pushDir = (otherPlayer.transform.position - transform.position).normalized;
        pushDir.y = 0.0f;

        float totalMass = _mass + otherPlayer._mass;
        float pushForce = _mass / totalMass;

        Vector3 pushVelocity = pushDir * pushForce * _moveSpeed;

        // 플레이어 밀기 로직 내부
        Quaternion originalRotation = otherPlayer.transform.rotation;

        otherPlayer._networkCharacterController.Move(pushVelocity * Runner.DeltaTime);

        // 강제로 회전값 복원 (회전 막기)
        otherPlayer.transform.rotation = originalRotation;
    }

    private void CheckCollision()
    {
        float height = _characterController.height;
        float radius = _characterController.radius;

        Vector3 center = transform.position + _characterController.center;
        Vector3 bottom = center + Vector3.down * (height / 2.0f - radius);
        Vector3 top = center + Vector3.up * (height / 2.0f - radius);

        Collider[] hits = Physics.OverlapCapsule(bottom, top, radius + 0.12f);

        foreach (Collider hit in hits)
        {
            // 자기 자신의 콜라이더 제외
            if (hit.gameObject != gameObject)
            {
                Player otherPlayer = hit.GetComponent<Player>();
                if (otherPlayer != null)
                {
                    PushOtherPlayer(otherPlayer);
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (_characterController == null) return;

        float height = _characterController.height;
        float radius = _characterController.radius;
        Vector3 center = transform.position + _characterController.center;

        // 캡슐의 top과 bottom을 정확하게 계산
        Vector3 bottom = center + Vector3.down * (height / 2.0f - radius);
        Vector3 top = center + Vector3.up * (height / 2.0f - radius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(top, radius);
        Gizmos.DrawWireSphere(bottom, radius);
        Gizmos.DrawLine(top + Vector3.forward * radius, bottom + Vector3.forward * radius);
        Gizmos.DrawLine(top - Vector3.forward * radius, bottom - Vector3.forward * radius);
        Gizmos.DrawLine(top + Vector3.right * radius, bottom + Vector3.right * radius);
        Gizmos.DrawLine(top - Vector3.right * radius, bottom - Vector3.right * radius);
    }
}