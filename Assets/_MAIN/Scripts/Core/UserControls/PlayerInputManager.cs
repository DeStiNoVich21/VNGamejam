using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


namespace DIALOGUE
{
    public class PlayerInputManager : MonoBehaviour
    {
        private PlayerInput input;
        private List<(InputAction action, Action<InputAction.CallbackContext> command)> actions = new List<(InputAction action, Action<InputAction.CallbackContext> command)>();

        void Awake()
        {
            input = GetComponent<PlayerInput>();
            InitializeActions();
        }

        private void InitializeActions()
        {
            actions.Add((input.actions["Next"], OnNext));
        }

        private void OnEnable()
        {
            foreach (var inputActions in actions)
            {
                inputActions.action.performed += inputActions.command;
            }
        }

        private void OnDisable()
        {
            foreach (var inputActions in actions)
            {
                inputActions.action.performed -= inputActions.command;
            }
        }

        public void OnNext(InputAction.CallbackContext c)
        {
            DialogueSystem.instance.OnUserPromt_Next();
        }
    }
}