﻿using System;
using System.Collections.Generic;

using static SDL2.SDL;

namespace Ryujinx.Input.SDL2
{
    public class SDL2GamepadDriver : IGamepadDriver
    {
        private Dictionary<int, string> _gamepadsInstanceIdsMapping;
        private List<string> _gamepadsIds;

        public ReadOnlySpan<string> GamepadsIds => _gamepadsIds.ToArray();

        public string DriverName => "SDL2";

        public event Action<string> OnGamepadConnected;
        public event Action<string> OnGamepadDisconnected;

        public SDL2GamepadDriver()
        {
            _gamepadsInstanceIdsMapping = new Dictionary<int, string>();
            _gamepadsIds = new List<string>();

            SDL2Driver.Instance.OnJoyStickConnected += HandleJoyStickConnected;
            SDL2Driver.Instance.OnJoystickDisconnected += HandleJoyStickDisconnected;

            SDL2Driver.Instance.Init();
        }

        private string GenerateGamepadId(int joystickIndex)
        {
            Guid guid = SDL_JoystickGetDeviceGUID(joystickIndex);

            if (guid == Guid.Empty)
            {
                return null;
            }

            return joystickIndex + "-" + guid.ToString();
        }

        private int GetJoystickIndexByGamepadId(string id)
        {
            string[] data = id.Split("-");

            if (data.Length != 6 || !int.TryParse(data[0], out int joystickIndex))
            {
                return -1;
            }

            return joystickIndex;
        }

        private void HandleJoyStickDisconnected(int joystickInstanceId)
        {
            if (_gamepadsInstanceIdsMapping.TryGetValue(joystickInstanceId, out string id))
            {
                _gamepadsInstanceIdsMapping.Remove(joystickInstanceId);
                _gamepadsIds.Remove(id);

                OnGamepadDisconnected?.Invoke(id);
            }
        }

        private void HandleJoyStickConnected(int joystickDeviceId, int joystickInstanceId)
        {
            // TODO: do not trust the ids sent here and scan for all gamepad.
            if (SDL_IsGameController(joystickDeviceId) == SDL_bool.SDL_TRUE)
            {
                string id = GenerateGamepadId(joystickDeviceId);

                if (id == null)
                {
                    return;
                }

                if(_gamepadsInstanceIdsMapping.TryAdd(joystickInstanceId, id))
                {
                    _gamepadsIds.Add(id);

                    OnGamepadConnected?.Invoke(id);
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                SDL2Driver.Instance.OnJoyStickConnected -= HandleJoyStickConnected;
                SDL2Driver.Instance.OnJoystickDisconnected -= HandleJoyStickDisconnected;

                // Simulate a full disconnect when disposing
                foreach (string id in _gamepadsIds)
                {
                    OnGamepadDisconnected?.Invoke(id);
                }

                _gamepadsIds.Clear();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public IGamepad GetGamepad(string id)
        {
            int joystickIndex = GetJoystickIndexByGamepadId(id);

            if (joystickIndex == -1)
            {
                return null;
            }

            IntPtr gamepadHandle = SDL_GameControllerOpen(joystickIndex);

            if (gamepadHandle == IntPtr.Zero)
            {
                return null;
            }

            if (id != GenerateGamepadId(joystickIndex))
            {
                SDL_GameControllerClose(gamepadHandle);

                return null;
            }

            return new SDL2Gamepad(gamepadHandle, id);
        }
    }
}