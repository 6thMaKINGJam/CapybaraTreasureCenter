using UnityEngine;

namespace Scripts.Managers
{
    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // 씬이 바뀌어도 유지
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 인터넷 연결 상태를 확인하는 메서드
        /// </summary>
        /// <returns>연결되어 있으면 true, 아니면 false</returns>
        public bool IsConnected()
        {
            // Application.internetReachability는 기기의 네트워크 상태를 반환합니다.
            // NotReachable이 아니라면 WIFI나 데이터 통신 중임을 의미합니다.
            return Application.internetReachability != NetworkReachability.NotReachable;
        }

        /// <summary>
        /// 현재 연결된 네트워크의 상세 종류 확인 (참고용)
        /// </summary>
        public void CheckNetworkStatus()
        {
            switch (Application.internetReachability)
            {
                case NetworkReachability.NotReachable:
                    Debug.Log("네트워크에 연결되어 있지 않습니다.");
                    break;
                case NetworkReachability.ReachableViaCarrierDataNetwork:
                    Debug.Log("모바일 데이터로 연결되었습니다.");
                    break;
                case NetworkReachability.ReachableViaLocalAreaNetwork:
                    Debug.Log("WIFI 혹은 LAN으로 연결되었습니다.");
                    break;
            }
        }
    }
}