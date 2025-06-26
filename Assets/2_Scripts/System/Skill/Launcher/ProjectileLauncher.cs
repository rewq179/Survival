using UnityEngine;

public class ProjectileLauncher : SkillLauncher
{
    public override SkillLauncherType Type => SkillLauncherType.Projectile;
    private const float MOVE_SPEED = 12.5f;
    private bool isHited = false;
    
    protected override void UpdateMovement()
    {
        if (isHited)
            return;

        float moveDistance = MOVE_SPEED * Time.deltaTime;
        transform.position += direction * moveDistance;

        // 최대 거리 체크
        float curDistance = (transform.position - startPosition).sqrMagnitude;
        if (curDistance >= range * range)
        {
            Deactivate();
            return;
        }

        if (Physics.Raycast(transform.position, direction, out RaycastHit hit, moveDistance, GameValue.UNIT_LAYERS))
        {
            OnHit(hit);
        }
    }

    private void OnHit(RaycastHit hit)
    {
        isHited = true;
        transform.position = hit.point;

        Unit target = hit.collider.GetComponent<Unit>();
        if (target != null)
        {
            OnHitTarget(target);
        }

        Deactivate();
    }
}