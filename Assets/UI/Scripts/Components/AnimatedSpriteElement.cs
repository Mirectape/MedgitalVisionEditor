using UnityEngine.UIElements;
using UnityEngine.U2D;
using UnityEngine;

namespace UI.Components
{
    public class AnimatedSpriteElement : Image
    {
        #region Public properties
        
        public bool IsAnimationRunning { get => _isAnimationRunning; }

        #endregion
        
        #region Private fields

        private SpriteAtlas _spriteAtlas;
        private IVisualElementScheduledItem _scheduler;
        private bool _isAnimationRunning = false;
        private int _currentFrameIndex = 0;
        private float _cycleDuration; // Общая длительность цикла анимации в секундах
        private int _frameDurationMilliseconds; // Длительность одного кадра в миллисекундах

        #endregion
        
        public AnimatedSpriteElement(SpriteAtlas atlas, float cycleDurationInSeconds)
        {
            if (atlas == null)
            {
                Debug.LogError("Atlas is null");
                return;
            }

            _spriteAtlas = atlas;
            _cycleDuration = cycleDurationInSeconds;

            // Рассчитываем длительность одного кадра в миллисекундах
            _frameDurationMilliseconds = Mathf.FloorToInt((_cycleDuration / _spriteAtlas.spriteCount) * 1000);
        }

        public void StartAnimation()
        {
            if (_scheduler == null)
            {
                _scheduler = this.schedule.Execute(UpdateSprite).Every(_frameDurationMilliseconds).StartingIn(0);
            }
            else if (!_isAnimationRunning)
            {
                _scheduler.Resume();
            }

            _isAnimationRunning = true;
        }

        public void StopAnimation()
        {
            if (_isAnimationRunning)
            {
                _scheduler.Pause();
                _isAnimationRunning = false;
            }
        }

        private void UpdateSprite()
        {
            this.sprite = _spriteAtlas.GetSprite(_currentFrameIndex.ToString());
            _currentFrameIndex = (_currentFrameIndex + 1) % _spriteAtlas.spriteCount;
        }

        public void SetCycleDuration(float duration)
        {
            _cycleDuration = duration;
            // Пересчитываем длительность одного кадра
            _frameDurationMilliseconds = Mathf.FloorToInt((_cycleDuration / _spriteAtlas.spriteCount) * 1000);

            // Обновляем планировщик, если анимация уже запущена
            if (_isAnimationRunning)
            {
                _scheduler.Every(_frameDurationMilliseconds);
            }
        }
    }
}