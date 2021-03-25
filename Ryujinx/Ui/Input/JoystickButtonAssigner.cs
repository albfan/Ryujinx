using System.Collections.Generic;
using System;
using System.IO;
using Ryujinx.Gamepad;

namespace Ryujinx.Ui.Input
{
    class JoystickButtonAssigner : ButtonAssigner
    {
        private IGamepad _gamepad;

        private GamepadStateSnapshot _currState;

        private GamepadStateSnapshot _prevState;

        private JoystickButtonDetector _detector;

        private bool _forStick;

        public JoystickButtonAssigner(IGamepad gamepad, float triggerThreshold, bool forStick)
        {
            _gamepad = gamepad;
            _detector = new JoystickButtonDetector();
            _forStick = forStick;

            _gamepad?.SetTriggerThreshold(triggerThreshold);
        }

        public void Init()
        {
            if (_gamepad != null)
            {
                _currState = _gamepad.GetStateSnapshot();
                _prevState = _currState;
            }    
        }

        public void ReadInput()
        {
            if (_gamepad != null)
            {
                _prevState = _currState;
                _currState = _gamepad.GetStateSnapshot();
            }

            CollectButtonStats();
        }

        public bool HasAnyButtonPressed()
        {
            return _detector.HasAnyButtonPressed();
        }

        public bool ShouldCancel()
        {
            // TODO: keyboard cancel
            return _gamepad == null || !_gamepad.IsConnected;
            // return Mouse.GetState().IsAnyButtonDown || Keyboard.GetState().IsAnyKeyDown;
        }

        public string GetPressedButton()
        {
            List<GamepadInputId> pressedButtons = _detector.GetPressedButtons();

            if (pressedButtons.Count > 0)
            {
                string result;

                if (!_forStick)
                {
                    result = pressedButtons[0].ToString();
                }
                else
                {
                    result = ((StickInputId)pressedButtons[0]).ToString();
                }

                return result;
            }

            return "";
        }

        private void CollectButtonStats()
        {
            if (_forStick)
            {
                for (StickInputId inputId = 0; inputId < StickInputId.Count; inputId++)
                {
                    (float x, float y) = _currState.GetStick(inputId);

                    float value;

                    if (x != 0.0f)
                    {
                        value = x;
                    }
                    else if (y != 0.0f)
                    {
                        value = y;
                    }
                    else
                    {
                        continue;
                    }

                    _detector.AddInput((GamepadInputId)inputId, value);
                }

            }
            else
            {
                for (GamepadInputId inputId = 0; inputId < GamepadInputId.Count; inputId++)
                {
                    if (_currState.IsPressed(inputId) && !_prevState.IsPressed(inputId))
                    {
                        _detector.AddInput(inputId, 1);
                    }

                    if (!_currState.IsPressed(inputId) && _prevState.IsPressed(inputId))
                    {
                        _detector.AddInput(inputId, -1);
                    }
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _gamepad?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private class JoystickButtonDetector
        {
            private Dictionary<GamepadInputId, InputSummary> _stats;

            public JoystickButtonDetector()
            {
                _stats = new Dictionary<GamepadInputId, InputSummary>();
            }

            public bool HasAnyButtonPressed()
            {
                foreach (var inputSummary in _stats.Values)
                {
                    if (CheckButtonPressed(inputSummary))
                    {
                        return true;
                    }
                }
                
                return false;
            }

            public List<GamepadInputId> GetPressedButtons()
            {
                List<GamepadInputId> pressedButtons = new List<GamepadInputId>();

                foreach (var kvp in _stats)
                {
                    if (!CheckButtonPressed(kvp.Value))
                    {
                        continue;
                    }
                    pressedButtons.Add(kvp.Key);
                }

                return pressedButtons;
            }

            public void AddInput(GamepadInputId button, float value)
            {
                InputSummary inputSummary;

                if (!_stats.TryGetValue(button, out inputSummary))
                {
                    inputSummary = new InputSummary();
                    _stats.Add(button, inputSummary);
                }

                inputSummary.AddInput(value);
            }

            public override string ToString()
            {
                TextWriter writer = new StringWriter();

                foreach (var kvp in _stats)
                {
                    writer.WriteLine($"Button {kvp.Key} -> {kvp.Value}");
                }

                return writer.ToString();
            }

            private bool CheckButtonPressed(InputSummary sequence)
            {
                float distance = Math.Abs(sequence.Min - sequence.Avg) + Math.Abs(sequence.Max - sequence.Avg);
                return distance > 1.5; // distance range [0, 2]
            }
        }

        private class InputSummary
        {
            public float Min, Max, Sum, Avg;

            public int NumSamples;

            public InputSummary()
            {
                Min = float.MaxValue;
                Max = float.MinValue;
                Sum = 0;
                NumSamples = 0;
                Avg = 0;
            }

            public void AddInput(float value)
            {
                Min = Math.Min(Min, value);
                Max = Math.Max(Max, value);
                Sum += value;
                NumSamples += 1;
                Avg = Sum / NumSamples;
            }

            public override string ToString()
            {
                return $"Avg: {Avg} Min: {Min} Max: {Max} Sum: {Sum} NumSamples: {NumSamples}";
            }
        }
    }
}
