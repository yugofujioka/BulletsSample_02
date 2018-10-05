using UnityEngine;


/// <summary>
/// 四方展開
/// </summary>
public sealed class GunFourExpand : MonoBehaviour {
	#region DEFINE
	private const float SHOT_SPAN = DEFINE.FRAME_TIME_60 * 360f; // 射撃間隔（F）
	private const float SHOT_SPEED = 580f;		// 弾速（pix./sec.）

	private const int WAY_WAY_COUNT = 4;        // WAYのWAY数
	private const float WAY_WAY_ANGLE = 90f;    // WAYのWAY間角度（deg.）
	private const int WAY_COUNT = 5;			// WAY数
	private const float WAY_ANGLE = 5f;         // WAY間角度（deg.）

	private const int EXTEND_COUNT = 2;			// 弾からWAY数
	private const float EXTEND_ANGLE = 100f;    // 弾からWAY間角度（deg.）
	private const float EXTEND_ADD_ANGLE = 22f; // 弾からWAY間加算角度（deg.）
	private const float EXTEND_SPAN = DEFINE.FRAME_TIME_60 * 2;  // 弾からWAY連射間隔（sec.）
	private const float EXPTEN_SPEED = 180f;

	private static readonly Vector3 ROT_AXIS = Vector3.back;

	private static readonly Quaternion WAY_WAY_START_ROT = Quaternion.AngleAxis(-0.5f * (WAY_WAY_ANGLE * (WAY_WAY_COUNT - 1)), ROT_AXIS);
	private static readonly Quaternion WAY_WAY_ROT = Quaternion.AngleAxis(WAY_WAY_ANGLE, ROT_AXIS);
	private static readonly Quaternion WAY_START_ROT = Quaternion.AngleAxis(-0.5f * (WAY_ANGLE * (WAY_COUNT - 1)), ROT_AXIS);
	private static readonly Quaternion WAY_ROT = Quaternion.AngleAxis(WAY_ANGLE, ROT_AXIS);
	#endregion


	#region MEMBER
	[SerializeField, Tooltip("主弾")]
	private Sprite mainBullet = null;
	[SerializeField, Tooltip("枝弾")]
	private Sprite branchBullet = null;

	private BulletLinear.ExtendProc extendTwoCross = null;
	#endregion


	#region MAIN FUNCTION
	/// <summary>
	/// 初期化
	/// </summary>
	public void Initialize() {
		this.extendTwoCross = new BulletLinear.ExtendProc(this.ExtendTwoCross);
	}

	/// <summary>
	/// 稼動
	/// </summary>
	public void Run(float elapsedTime) { }
	#endregion


	#region PUBLIC FUNCTION
	/// <summary>
	/// 射撃開始
	/// </summary>
	public void PullTrigger() {
		Vector3 shotDirect = Vector3.down;
		
		// 射線計算
		Vector3 dir = WAY_WAY_START_ROT * shotDirect;

		Vector3 point = Camera.main.WorldToScreenPoint(this.transform.localPosition);
		point.x -= Screen.width * 0.5f;
		point.y -= Screen.height * 0.5f;
		point.z = 0f;

		BulletLinear bullet = null;
		for (int i = 0; i < WAY_WAY_COUNT; ++i) {
			Vector3 emitDir = WAY_START_ROT * dir;
			for (int emit = 0; emit < WAY_COUNT; ++emit) {
				if (GameManager.bulletManager.AwakeObject(0, point, out bullet)) {
					BulletLinear bl = bullet as BulletLinear;
					bl.ExtendCallback(this.extendTwoCross); // MEMO: passedTimeがあるのでShoot前に設定
					bl.genericFloat[0] = DEFINE.FRAME_TIME_60;
					bl.genericFloat[1] = EXTEND_ANGLE;
					bl.Shoot(this.mainBullet, SHOT_SPEED, 0f, ref emitDir, 0f);
					bl.sortingLayer = SortingLayer.NameToID("BulletMain");
				}

				emitDir = WAY_ROT * emitDir;
			}

			dir = WAY_WAY_ROT * dir;
		}
	}

	/// <summary>
	/// 射撃停止
	/// </summary>
	public void ReleaseTrigger() { }
	#endregion


	#region PRIVATE FUNCTION
	/// <summary>
	/// 2way交差弾拡張
	/// </summary>
	/// <param name="elapsedTime">経過時間</param>
	/// <param name="bullet">親弾</param>
	private void ExtendTwoCross(float elapsedTime, BulletLinear emitter) {
		emitter.genericFloat[0] -= elapsedTime;
		if (emitter.genericFloat[0] > DEFINE.FLOAT_MINIMUM)
			return;

		float passedTime = -emitter.genericFloat[0];
		emitter.genericFloat[0] = EXTEND_SPAN;

		Quaternion startRot = Quaternion.AngleAxis(-0.5f * (emitter.genericFloat[1] * (EXTEND_COUNT - 1)), ROT_AXIS);
		Quaternion rot = Quaternion.AngleAxis(emitter.genericFloat[1], ROT_AXIS);

		// 射線計算
		Vector3 dir = startRot * emitter.direct;

		BulletLinear bullet = null;
		for (int i = 0; i < EXTEND_COUNT; ++i) {
			Vector3 point = emitter.move + dir * 12f; // 細長いので発生位置をズラす
			if (GameManager.bulletManager.AwakeObject(0, point, out bullet)) {
				BulletLinear bl = bullet as BulletLinear;
				bl.Shoot(this.branchBullet, EXPTEN_SPEED, 0f, ref dir, passedTime);
				bl.sortingLayer = SortingLayer.NameToID("BulletBranch");
			}
			dir = rot * dir;
		}
		emitter.genericFloat[1] += EXTEND_ADD_ANGLE;
		if (emitter.genericFloat[1] > 945f)
			emitter.ExtendCallback(null);
	}
	#endregion
}
