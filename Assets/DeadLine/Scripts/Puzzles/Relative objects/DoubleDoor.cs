using UnityEngine;

namespace DeadLine
{
    public class DoubleDoor : MonoBehaviour, IRelativeObjects
    {
        [SerializeField] private GameObject leftDoorObject;
        [SerializeField] private GameObject rightDoorObject;

        [Header("SFX")]
        [SerializeField] private bool playDoorSfx = true;
        [SerializeField] private string sfxOpen = "DoorOpenCreak";
        [SerializeField] private string sfxClose = "DoorCloseCreak";

        private IRelativeObjects _leftDoor;
        private IRelativeObjects _rightDoor;

        private bool _isOpen = false;

        private void Awake()
        {
            if (leftDoorObject != null)
                _leftDoor = leftDoorObject.GetComponent<IRelativeObjects>();

            if (rightDoorObject != null)
                _rightDoor = rightDoorObject.GetComponent<IRelativeObjects>();
        }

        public void action()
        {
            if (!_isOpen)
            {
                _isOpen = true;

                if (playDoorSfx && !string.IsNullOrWhiteSpace(sfxOpen))
                    AudioManager.Instance?.PlayOneShotSound(sfxOpen);
            }

            if (_leftDoor != null && _leftDoor.checkStatu()) _leftDoor.action();
            if (_rightDoor != null && _rightDoor.checkStatu()) _rightDoor.action();
        }

        public void action(object obj) => action();

        public void Noaction()
        {
            if (_isOpen)
            {
                _isOpen = false;

                if (playDoorSfx && !string.IsNullOrWhiteSpace(sfxClose))
                    AudioManager.Instance?.PlayOneShotSound(sfxClose);
            }

            if (_leftDoor != null && _leftDoor.checkStatu()) _leftDoor.Noaction();
            if (_rightDoor != null && _rightDoor.checkStatu()) _rightDoor.Noaction();
        }

        public void Noaction(object obj) => Noaction();

        public bool checkStatu()
        {
            bool ok = true;
            if (_leftDoor != null) ok &= _leftDoor.checkStatu();
            if (_rightDoor != null) ok &= _rightDoor.checkStatu();
            return ok;
        }

        public bool UseParam() => false;
    }
}
