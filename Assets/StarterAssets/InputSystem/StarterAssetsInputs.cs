using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{


	public class StarterAssetsInputs : MonoBehaviour
	{
		[Header("Character Input Values")]
		public Vector2 move;
		public Vector2 look;
		public bool jump;
		public bool sprint;
		public bool action;
		public bool talk;
		public bool info;
		public InfoState infoState;
		public bool attack;
		public bool aim;

		public static event Action OnInfoKey;

		[Header("Movement Settings")]
		public bool analogMovement;

		[Header("Mouse Cursor Settings")]
		public bool cursorLocked = true;
		public bool cursorInputForLook = true;

#if ENABLE_INPUT_SYSTEM
		public void OnMove(InputValue value)
		{
			if(!info)
				MoveInput(value.Get<Vector2>());
		}

		public void OnLook(InputValue value)
		{
			if(cursorInputForLook && !info)
			{
				LookInput(value.Get<Vector2>());
			}
		}

		public void OnJump(InputValue value)
		{
			if(!info)
				JumpInput(value.isPressed);
		}

		public void OnSprint(InputValue value)
		{
			SprintInput(value.isPressed);
		}

		public void OnAction(InputValue value)
		{

		}
		
		public void OnTalk(InputValue value)
		{

		}
		
		public void OnInfo(InputValue value)
		{
			InfoSateInfo(value.isPressed, InfoState.Info);
        }

		public void OnInventory(InputValue value)
		{
            InfoSateInfo(value.isPressed, InfoState.Inventory);
        }

		public void OnSkill(InputValue value)
		{
            InfoSateInfo(value.isPressed, InfoState.Skill);
        }

		public void OnMap(InputValue value)
		{
            InfoSateInfo(value.isPressed, InfoState.Map);
        }

		public void OnAttack(InputValue value)
		{

		}

		public void OnAim(InputValue value)
		{

		}
#endif

		public void MoveInput(Vector2 newMoveDirection)
		{
			move = newMoveDirection;
		} 

		public void LookInput(Vector2 newLookDirection)
		{
			look = newLookDirection;
		}

		public void JumpInput(bool newJumpState)
		{
			jump = newJumpState;
		}

		public void SprintInput(bool newSprintState)
		{
			sprint = newSprintState;
		}

		public void InfoSateInfo(bool newInfoState, InfoState state)
		{
			if(!info)
			{
				infoState = state;
				info = true;
				move = Vector2.zero;
				jump = false;
			}
			else
			{
				if (infoState != state)
					infoState = state;
				else
					info = false;
			}
			OnInfoKey.Invoke();
		}


		private void OnApplicationFocus(bool hasFocus)
		{
			SetCursorState(cursorLocked);
		}

		private void SetCursorState(bool newState)
		{
			Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
		}
	}
	
}