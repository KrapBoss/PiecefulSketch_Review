using System.Text;
using UnityEngine;
using static Fusion.Sockets.NetBitBuffer;
using static UnityEngine.RuleTile.TilingRuleOutput;

namespace Custom
{
    public static class CustomCalculator
    {
        //화면 크기만큼의 바운더리를 구분하여 반환한다.
        public static Vector3 Clamp(Vector2 position, Vector2 leftBottom, Vector2 RightTop)
        {
            Vector3 pos = position;
            pos.x = Mathf.Clamp(pos.x, leftBottom.x, RightTop.x);
            pos.y = Mathf.Clamp(pos.y, leftBottom.y, RightTop.y);
            pos.z = 0;
            return pos;
        }

        /// <summary>
        /// 지정된 화면의 크기만큼 가로 세로의 크기값을 반환합니다.
        /// 최대값은 가로 세로 중 가장 작은 값을 기준으로 정해집니다.
        /// </summary>
        /// <param name="ratio">최대 사이즈에서 감소시킬 비율</param>
        /// <param name="sizeX">이미지의 가로 사이즈</param>
        /// <param name="sizeY">이미지의 세로 사이즈</param>
        /// <returns></returns>
        public static (float w, float h) GetBoundSize(float ratio, float sizeX, float sizeY)
        {
            float _ratio = sizeX / sizeY;

            float _screenRatio = Screen.width / (float)Screen.height;

            float width, height;

            // 가로 화면
            if (_screenRatio > 1)
            {
                height = Screen.height;
                width = height * _ratio;
            }
            //세로 화면
            else
            {
                width = Screen.width;
                height = width / _ratio;
            }

            return (width* ratio, height* ratio);
        }

        /// <summary>
        /// 최대 사이즈에 따른 이미지의 크기를 반환합니다.
        /// </summary>
        /// <param name="ratio">최종적으로 구해진 값에 곱할 값</param>
        /// <returns></returns>
        public static (float w, float h) GetBoundSizeWithSize(Sprite sprite, float maxSize, float ratio = 1.0f)
        {
            float _ratio = sprite.bounds.size.x / sprite.bounds.size.y;

            float width, height;

            // 세로가 더 크다
            if (_ratio < 1)
            {
                height = maxSize;
                width = height * _ratio;
            }
            //가로가 더 크다
            else
            {
                width = maxSize;
                height = width / _ratio;
            }

            return (width * ratio, height * ratio);
        }

        static StringBuilder str = new StringBuilder();
        /// <summary>
        /// 시간을 "H시 M분 S초" 형식으로 변환하며, 10 미만일 경우 앞의 0을 표시하지 않습니다.
        /// </summary>
        /// <param name="timeInSeconds">변환할 초 단위 시간</param>
        /// <returns>앞자리 0이 제거된 포맷팅 문자열</returns>
        public static string FormatTime(float timeInSeconds)
        {
            // 소수점 이하 버림 처리
            int totalSeconds = (int)Mathf.Floor(timeInSeconds);

            int hours = totalSeconds / 3600;
            int minutes = (totalSeconds % 3600) / 60;
            int seconds = totalSeconds % 60;

            str.Clear();

            // 1. 시간 표시 (0보다 클 때만, 최대 2자리 자동 유지)
            if (hours > 0)
            {
                str.Append($"{hours}{Localization.Localize("Hour")} ");
            }

            // 2. 분 표시 (시간이 있거나 분이 0보다 클 때)
            if (minutes > 0 || hours > 0)
            {
                // 제작 의도: :D2를 제거하여 1~9분일 때 '01'이 아닌 '1'로 표시함
                str.Append($"{minutes}{Localization.Localize("Min")} ");
            }

            // 3. 초 표시 (항상 표시, 1~9초일 때 '1'로 표시)
            // Unity 효율성: StringBuilder와 문자열 보간을 사용하여 힙 메모리 할당 최적화
            str.Append($"{seconds}{Localization.Localize("Sec")}");

            return str.ToString();
        }

        /// <summary>
        /// 타켓 렉트 사이즈에 따른 스프라이트 사이즈를 조정합니다.
        /// </summary>
        /// <param name="baseRect"></param>
        /// <param name="spriteSize"></param>
        /// <returns></returns>
        public static Vector2 GetSpriteSize(RectTransform baseRect, (float sprX, float sprY) spriteSize)
        {
            // 1. 이미지 및 Rect 정보 추출
            float x = spriteSize.sprX;
            float y = spriteSize.sprY;
            float spriteRatio = x / y;

            // rect.rect를 사용하면 앵커 설정과 관계없이 실제 렌더링 사이즈를 가져옵니다.
            float rx = baseRect.rect.width;
            float ry = baseRect.rect.height;
            float rectRatio = rx / ry;

            // 2. 비율 비교 및 사이즈 결정 (Aspect Fill 로직)
            if (spriteRatio > rectRatio)
            {
                return new Vector2(rx, rx / spriteRatio);
            }
            else
            {
                // 이미지가 컨테이너보다 가로로 더 긴 경우: 세로를 맞추고 가로를 키움
                return new Vector2(ry * spriteRatio, ry);
            }
        }


        /// <summary>
        /// 타켓 렉트 사이즈에 따른 스프라이트 사이즈를 조정합니다.
        /// </summary>
        /// <param name="baseRect"></param>
        /// <param name="spriteSize"></param>
        /// <returns></returns>
        public static Vector2 GetSpriteSize((float x, float y) baseRect, (float sprX, float sprY) spriteSize)
        {
            // 1. 이미지 및 Rect 정보 추출
            float x = spriteSize.sprX;
            float y = spriteSize.sprY;
            float spriteRatio = x / y;

            // rect.rect를 사용하면 앵커 설정과 관계없이 실제 렌더링 사이즈를 가져옵니다.
            float rx = baseRect.x;
            float ry = baseRect.y;
            float rectRatio = rx / ry;

            // 2. 비율 비교 및 사이즈 결정 (Aspect Fill 로직)
            if (spriteRatio > rectRatio)
            {
                // 이미지가 컨테이너보다 가로로 더 긴 경우: 세로를 맞추고 가로를 키움
                return new Vector2(ry * spriteRatio, ry);
            }
            else
            {
                // 이미지가 컨테이너보다 세로로 더 긴 경우: 가로를 맞추고 세로를 키움
                return new Vector2(rx, rx / spriteRatio);
            }
        }

        /// <summary>
        /// 코인 단위를 변환하여 반환합니다.
        /// </summary>
        /// <param name="coin"></param>
        /// <returns></returns>
        public static string TFCoinString(int coin)
        {
            return $"{coin:N0}";
        }
    }
}
