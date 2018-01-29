using UnityEngine;  
using UnityEngine.UI;  
using System;  
using System.Collections;  
using System.Collections.Generic; 

namespace StarForce{

	/// <summary>  
	/// @desc:       滑动列表单个组件  
	/// @author:     Rambo  
	/// </summary>  
	[DisallowMultipleComponent]  
	public class TableViewItem : MonoBehaviour  
	{  
		public int index;  
		/// <summary>  
		/// 设置小孩下标  
		/// </summary>  
		public int Index  
		{  
			set  
			{  
				index = value;  
			}  
			get  
			{  
				return index;  
			}  
		}  

	}  
    public delegate void TableViewAction(GameObject obj ,int index);

/// <summary>  
/// @desc:       动态创建滑动列表，通过ugui5.3.6 通过创建Scroll View  
/// @author:     Rambo  
/// @use         必须初始化scrollRect，content，itemPrefab,其他public参数按需初始；  
/// @use         使用代码添加组件时，调用InitTableView进行初始化  
/// @use         调用InitList创建tableView, setItemCall回调创建  
/// </summary>  
[DisallowMultipleComponent]  
	public class TableView : MonoBehaviour  
	{  
		public enum Arrangement  
		{  
			Horizontal,  
			Vertical,  
		}  
		/// <summary>  
		/// 选择滑动方向  
		/// </summary>  
		public Arrangement arrangement = Arrangement.Vertical;  

		/// <summary>  
		/// item刷新回调，声明了一个Delegate对象  
		/// </summary>  
		public Action<GameObject, int> SetItemCall;  

		/// <summary>  
		/// 设置行列，根据方向，垂直滑动为列，横向滑动为行  
		/// </summary>  
		[Range(1, 50)]  
		public int maxPerLine = 1;  

		/// <summary>  
		/// cells的宽间距  
		/// </summary>  
		[Range(0, 50)]  
		public float cellWidthSpace = 0f;  

		/// <summary>  
		/// cells的高间距  
		/// </summary>  
		[Range(0, 50)]  
		public float cellHeightSpace = 0f;  

		/// <summary>  
		/// 可见区域显示  
		/// </summary>  
		[Range(0, 30)]  
		public int viewCount = 5;  

		/// <summary>  
		/// 额外可见区域显示  
		/// </summary>  
		[Range(-5, 5)]  
		public int extraViewCount = 0;  

		/// <summary>  
		/// 左间距  
		/// </summary>  
		[Range(0, 100)]  
		public int marginLeft = 0;  

		/// <summary>  
		/// 上间距  
		/// </summary>  
		[Range(0, 100)]  
		public int marginTop = 0;  

		public ScrollRect scrollRect;  
		public RectTransform content;  
		public GameObject itemPrefab;  

		private float cellWidth = 100f;             //cells的宽  
		private float cellHeight = 100f;            //cells的高  
		private int dataCount;                      //创建最大数量  
		private int curScrollPerLineIndex = -1;     //当前滑动下标  
		private float offx;                         //位置偏移量x  
		private float offy;                         //位置偏移量y  
		private List<TableViewItem> listItem = new List<TableViewItem>(); //存储显示的cells  
		private Queue<TableViewItem> unUseItem = new Queue<TableViewItem>();//存储删除的cells  
        private Coroutine co = null; // 用于更新数据的协程

        private  TableViewAction updateFunc;//用于更新item的内容

		#region 初始化处理  
		/// <summary>  
		/// 检测是否为空值  
		/// </summary>  
		private bool CheckArgeIsNull()  
		{  
			if (content == null)  
			{  
				return true;  
			}  

			if (scrollRect == null)  
			{  
				return true;  
			}  

			if (itemPrefab == null)  
			{  
				return true;  
			}  

			cellWidth = itemPrefab.GetComponent<RectTransform>().sizeDelta.x;  
			cellHeight = itemPrefab.GetComponent<RectTransform>().sizeDelta.y;  

			offx = -(content.sizeDelta.x - cellWidth) / 2;  
			offy = (content.sizeDelta.y - cellHeight) / 2;  

			return false;  
		}  

		/// <summary>  
		/// 设置小孩数量  
		/// </summary>  
		/// <param name="count">小孩数量</param>  
		private void SetDataCount(int count)  
		{  
			if (dataCount == count)  
			{  
				return;  
			}  
			dataCount = count;  

			//设置content描点,防止改变大小时移动  
			switch (arrangement)  
			{  
			case Arrangement.Horizontal:  
				//设置内容面板锚点，对齐方式，横向滑动为向左对齐  
				content.anchorMin = new Vector2(0f, 0.5f);  
				content.anchorMax = new Vector2(0f, 0.5f);  
				content.pivot = new Vector2(0f, 0.5f);  

				break;  
			case Arrangement.Vertical:  
				//设置内容面板锚点，对齐方式，纵向滑动为向上对齐  
				content.anchorMin = new Vector2(0.5f, 1f);  
				content.anchorMax = new Vector2(0.5f, 1f);  
				content.pivot = new Vector2(0.5f, 1f);  

				break;  
			}  
		}  

		/// <summary>  
		/// 更新Content SizeDelta  
		/// </summary>  
		private void SetUpdateContentSize()  
		{  
			int lineCount = (int)Mathf.Ceil((float)dataCount / maxPerLine) + extraViewCount;  
			switch (arrangement)  
			{  
			case Arrangement.Horizontal:  
				content.sizeDelta = new Vector2(cellWidth * lineCount + cellWidthSpace * (lineCount - 1), content.sizeDelta.y);  
				//content.anchoredPosition = Vector2.zero;  
				break;  
			case Arrangement.Vertical:  
				content.sizeDelta = new Vector2(content.sizeDelta.x, cellHeight * lineCount + cellHeightSpace * (lineCount - 1));  
				//content.anchoredPosition = Vector2.zero;  
				break;  
			}  


		}  

		/// <summary>  
		/// 延时设置ContentSize  
		/// </summary>  
		/// <returns></returns>  
		IEnumerator StartContinue()  
		{  

			//Vector3 oldPos = content.transform.localPosition;  
			//content.transform.localPosition = new Vector3(oldPos.x, 0, oldPos.z);  
			yield return new WaitForSeconds(1.0f);  
			SetUpdateContentSize();  

		}  
		#endregion  

		#region 滑动改变处理  
		/// <summary>  
		/// 滑动改变监听  
		/// </summary>  
		/// <param name="vt2">滑动变化参数，0~1之间变化</param>  
		private void OnValueChanged(Vector2 vt2)  
		{  
			/*switch (arrangement)  
			{  
			case Arrangement.Vertical:  
				float y = vt2.y;  
				if (y >= 1.0f || y <= 0.0f)  
				{  
					return;  
				}  
				break;  
			case Arrangement.Horizontal:  
				float x = vt2.x;  
				if (x <= 0.0f || x >= 1.0f)  
				{  
					return;  
				}  
				break;  
			}  */
			int _curScrollPerLineIndex = GetCurScrollPerLineIndex();  
			if (_curScrollPerLineIndex == curScrollPerLineIndex)  
			{  
				return;  
			}  

            if (null != co) {
                StopCoroutine (co);
            }
            co = StartCoroutine(SetUpdateRectItem(_curScrollPerLineIndex));  
		}  


		/// <summary>  
		/// 设置更新区域内item, 1.隐藏区域之外对象, 2.更新区域内数据  
		/// </summary>  
		/// <param name="scrollPerLineIndex">当前滑动到的下标</param>  
        public IEnumerator SetUpdateRectItem(int scrollPerLineIndex)  
		{  

			if (scrollPerLineIndex < 0)  
			{  
                yield return null;  
			}  
			curScrollPerLineIndex = scrollPerLineIndex;  
			int startDataIndex = curScrollPerLineIndex * maxPerLine;  
			int endDataIndex = (curScrollPerLineIndex + viewCount) * maxPerLine;  

			//Log.Instance.infoFormat ("update rectitem, star:{0}, end:{1}, scrollIndex:{2}", startDataIndex, endDataIndex, scrollPerLineIndex);

			// 移除  
			for (int i = listItem.Count - 1; i >= 0; i--)  
			{  
				TableViewItem item = listItem[i];  
				int index = item.Index;  
				if (index < startDataIndex || index >= endDataIndex)  
				{  
					item.Index = -1;  
					unUseItem.Enqueue(item);  
					listItem.Remove(item);  
//                    if (item.gameObject.activeSelf) {
//                        item.gameObject.SetActive (false);
//                    }
					//Log.Instance.infoFormat ("remove item, index:{0}", index);
				}  
			}  

			// 显示  
			for (int dataIndex = startDataIndex; dataIndex < endDataIndex; dataIndex++)  
			{  
				if (dataIndex >= dataCount)  
				{  
					continue;  
				}  
				if (IsExistDataByDataIndex(dataIndex))  
				{  
					continue;  
				}  
				CreateItem(dataIndex);  
                yield return null;
				//Log.Instance.infoFormat ("create item, index:{0}", dataIndex);
			}  
		}  

		/// <summary>  
		/// 当前数据是否存在List中  
		/// </summary>  
		/// <param name="dataIndex">下标0开始</param>  
		/// <returns></returns>  
		private bool IsExistDataByDataIndex(int dataIndex)  
		{  
			if (listItem == null || listItem.Count <= 0)  
			{  
				return false;  
			}  
			for (int i = 0; i < listItem.Count; i++)  
			{  
				if (listItem[i].Index == dataIndex)  
				{  
					return true;  
				}  
			}  
			return false;  
		}  


		/// <summary>  
		/// 根据Content偏移,计算当前开始显示所在数据列表中的行或列  
		/// </summary>  
		/// <returns></returns>  
		private int GetCurScrollPerLineIndex()  
		{  
			switch (arrangement)  
			{  
			case Arrangement.Horizontal: //水平方向  
				return Mathf.FloorToInt(Mathf.Abs(content.anchoredPosition.x) / (cellWidth + cellWidthSpace));  
			case Arrangement.Vertical://垂着方向  
				return Mathf.FloorToInt(Mathf.Abs(content.anchoredPosition.y) / (cellHeight + cellHeightSpace));  
			}  
			return 0;  
		}  
		#endregion  

		#region 小孩创建处理  
		/// <summary>  
		/// 创建元素  
		/// </summary>  
		/// <param name="dataIndex">下标，从0开始</param>  
		private void CreateItem(int dataIndex)  
		{  
			TableViewItem item;  
			if (unUseItem.Count > 0)  
			{  
				item = unUseItem.Dequeue(); 
//                item.gameObject.SetActive (true);
			}  
			else  
			{  
				item = AddChild(itemPrefab, content).AddComponent<TableViewItem>();  
			}  
			item.Index = dataIndex;  
			SetPostion(item);  
			listItem.Add(item);  
		}  

		private void SetPostion(TableViewItem item)  
		{  
			var index = item.Index;  
			if (index >= 0)  
			{  
				RectTransform cellRectTrans = item.gameObject.GetComponent<RectTransform>();  
				switch (arrangement)  
				{  
				case TableView.Arrangement.Vertical:  
					//float pivotX = maxPerLine == 1 ? 0.5f : 1.0f; //多行多列时候向左靠齐  
					cellRectTrans.anchorMin = new Vector2(0.5f, 1f);  
					cellRectTrans.anchorMax = new Vector2(0.5f, 1f);  
					cellRectTrans.pivot = new Vector2(0.5f, 1f);  
					break;  
				case TableView.Arrangement.Horizontal:  
					cellRectTrans.anchorMin = new Vector2(0f, 0.5f);  
					cellRectTrans.anchorMax = new Vector2(0f, 0.5f);  
					cellRectTrans.pivot = new Vector2(0f, 0.5f);  
					break;  
				}  
				cellRectTrans.localPosition = GetLocalPositionByIndex(index);  
				//cellRectTrans.anchoredPosition3D  
	//			if (SetItemCall != null)  
	//			{  
	//				item.gameObject.name = (index < 10) ? ("0" + index) : ("" + index);  
	//				SetItemCall(item.gameObject, index);  
	//			}  
				if (updateFunc != null)
				{
					updateFunc (item.gameObject, index);
				}

			}  
		}  

		/// <summary>  
		/// 实例化预设对象 、添加实例化对象到指定的子对象下  
		/// </summary>  
		private GameObject AddChild(GameObject goPrefab, Transform parent)  
		{  
			if (goPrefab == null || parent == null)  
			{  
				return null;  
			}  
			GameObject goChild = Instantiate(goPrefab) as GameObject;  
			//goChild.layer = parent.gameObject.layer;  
			goChild.SetActive(true);  
			goChild.transform.SetParent(parent, false);  

			return goChild;
		}  
		#endregion  

		#region 公共接口  
		/// <summary>  
		/// 添加当前数据索引数据  
		/// </summary>  
		/// <param name="dataIndex">下标，从0开始</param>  
		public void AddItem(int dataIndex)  
		{  
			if (dataIndex < 0 || dataIndex > dataCount)  
			{  
				return;  
			}  
			//检测是否需添加gameObject  
			bool isNeedAdd = false;  
			for (int i = listItem.Count - 1; i >= 0; i--)  
			{  
				TableViewItem item = listItem[i];  
				if (item.Index >= (dataCount - 1))  
				{  
					isNeedAdd = true;  
					break;  
				}  
			}  
			SetDataCount(dataCount + 1);  

			if (isNeedAdd)  
			{  
				for (int i = 0; i < listItem.Count; i++)  
				{  
					TableViewItem item = listItem[i];  
					int oldIndex = item.Index;  
					if (oldIndex >= dataIndex)  
					{  
						item.Index = oldIndex + 1;  
					}  
					item = null;  
				}  

                if (null != co) {
                    StopCoroutine (co);
                }
                co = StartCoroutine(SetUpdateRectItem(GetCurScrollPerLineIndex()));  
			}  
			else  
			{  
				//重新刷新数据  
				for (int i = 0; i < listItem.Count; i++)  
				{  
					TableViewItem item = listItem[i];  
					int oldIndex = item.Index;  
					if (oldIndex >= dataIndex)  
					{  
						item.Index = oldIndex;  
					}  
					item = null;  
				}  
			}  

		}  

		/// <summary>  
		/// 删除当前数据索引下数据  
		/// </summary>  
		/// <param name="dataIndex">索引下标，从0开始</param>  
		public void DelItem(int dataIndex)  
		{  
			if (dataIndex < 0 || dataIndex >= dataCount)  
			{  
				return;  
			}  
			//删除item逻辑三种情况  
			//1.只更新数据，不销毁gameObject,也不移除gameobject  
			//2.更新数据，且移除gameObject,不销毁gameObject  
			//3.更新数据，销毁gameObject  

			bool isNeedDestroyGameObject = (listItem.Count >= dataCount);  
			SetDataCount(dataCount - 1);  

			for (int i = listItem.Count - 1; i >= 0; i--)  
			{  
				TableViewItem item = listItem[i];  
				int oldIndex = item.Index;  
				if (oldIndex == dataIndex)  
				{  
					listItem.Remove(item);  
					if (isNeedDestroyGameObject)  
					{  
						GameObject.Destroy(item.gameObject);  
					}  
					else  
					{  
						item.Index = -1;  
						unUseItem.Enqueue(item);  
					}  
				}  
				if (oldIndex > dataIndex)  
				{  
					item.Index = oldIndex - 1;  
				}  
			}  

            if (null != co) {
                StopCoroutine (co);
            }
            co = StartCoroutine(SetUpdateRectItem(GetCurScrollPerLineIndex()));  
		}  


		/// <summary>  
		/// 获取当前index下对应Content下的本地坐标  
		/// </summary>  
		/// <param name="index">下标，从0开始</param>  
		/// <returns></returns>  
		public Vector3 GetLocalPositionByIndex(int index)  
		{  
			float x = 0f;  
			float y = 0f;  
			float z = 0f;  
			switch (arrangement)  
			{  
			case Arrangement.Horizontal: //水平方向  
				x = (index / maxPerLine) * (cellWidth + cellWidthSpace) + marginLeft;  
				y = -(index % maxPerLine) * (cellHeight + cellHeightSpace) + offy - marginTop;  
				break;  
			case Arrangement.Vertical://垂着方向  
				x = (index % maxPerLine) * (cellWidth + cellWidthSpace) + offx + marginLeft;  
				y = -(index / maxPerLine) * (cellHeight + cellHeightSpace) - marginTop;  
				break;  
			}  
			return new Vector3(x, y, z);  
		}  

		/// <summary>
		/// 获取滚动位置
		/// </summary>
		/// <returns>The scroll position.</returns>
		public float GetScrollPosition() {
			switch (arrangement) {
			case Arrangement.Horizontal:
				return scrollRect.horizontalNormalizedPosition;
			case Arrangement.Vertical:
				return scrollRect.verticalNormalizedPosition;
			}
			return 0f;
		}

		/// <summary>
		/// 滚动到制定位置
		/// </summary>
		/// <param name="pos">Position.</param>
		public void ScrollToPosition(float pos) {
			switch (arrangement) {
			case Arrangement.Horizontal:
				scrollRect.horizontalNormalizedPosition = pos;
				break;
			case Arrangement.Vertical:
				scrollRect.verticalNormalizedPosition = pos;
				break;
			}
		}


		/// <summary>  
		/// 获取所在的下标  
		/// </summary>  
		/// <param name="itemObj">对象</param>  
		/// <returns></returns>  
		public int IndexOf(GameObject itemObj)  
		{  
			TableViewItem item = itemObj.gameObject.GetComponent<TableViewItem>();  
			return listItem.IndexOf(item);  
		}  
		#endregion  

		#region 初始化接口  
		/// <summary>  
		/// 动态初始化tableview  
		/// </summary>  
		/// <param name="rect">滑动区域，监听滑动</param>  
		/// <param name="cont">滑动列表容器</param>  
		/// <param name="prefab">滑动cell</param>  
        public void InitTableView(GameObject prefab,int maxCount,TableViewAction updateFunc)  
		{  
            if (prefab != null)
            {
                itemPrefab = prefab;  
            }
           
			this.updateFunc = updateFunc;
			if (CheckArgeIsNull())  
			{  
				return;  
			}  
			InitList (maxCount);
		}  

		/// <summary>  
		/// 重设列表  
		/// </summary>  
		/// <param name="dataCount">列表长度</param>  
		public void ReLoad(int dataCount)  
		{  
			// Log.Instance.infoFormat ("reload data {0}, listSize:{1}", dataCount, listItem.Count);
			// 移除监听
			scrollRect.onValueChanged.RemoveAllListeners();  

            // 参数检查
            CheckArgeIsNull();

			//移除  
			for (int i = listItem.Count - 1; i >= 0; i--)  
			{  
				TableViewItem item = listItem[i];  
				GameObject.Destroy(item.gameObject);  
			}  
			while (unUseItem.Count > 0) {
				TableViewItem item = unUseItem.Dequeue ();
				GameObject.Destroy(item.gameObject);  
			}

			SetDataCount(dataCount);  

			unUseItem.Clear();  
			listItem.Clear();  

            scrollRect.normalizedPosition = Vector2.one;

            if (null != co) {
                StopCoroutine (co);
            }
            co = StartCoroutine(SetUpdateRectItem(0));  

            // 避免切换时候滚动现象出现
            SetUpdateContentSize ();

			scrollRect.onValueChanged.AddListener(OnValueChanged);

			// StartCoroutine(StartContinue());  
			//SetUpdateContentSize();
		}  


		/// <summary>  
		/// 初始化列表  
		/// </summary>  
		/// <param name="dataCount">列表长度</param>  
		public void InitList(int dataCount)  
		{  
			if (CheckArgeIsNull())  
			{  
				return;  
			}  

			if (dataCount <= 0)  
			{  
				return;  
			}  

			SetDataCount(dataCount);  

			unUseItem.Clear();  
			listItem.Clear();  

            if (null != co) {
                StopCoroutine (co);
            }
            co = StartCoroutine(SetUpdateRectItem(0));  

			scrollRect.onValueChanged.RemoveAllListeners();  
			scrollRect.onValueChanged.AddListener(OnValueChanged);  

			SetUpdateContentSize();
		}  

		/// <summary>
		/// 更新数据
		/// </summary>
		public void ReloadData() {
			for (int i = 0; i < listItem.Count; i++) {
				TableViewItem item = listItem [i];
				if (null != updateFunc) {
					updateFunc (item.gameObject, item.Index);
				}
			}
		}

		/// <summary>
		/// 更新数据
		/// </summary>
		public void ReloadData(int dataIndex) {
			if (listItem.Count > dataIndex) {
				TableViewItem item = listItem [dataIndex];
				if (null != updateFunc) {
					updateFunc (item.gameObject, item.Index);
				}
			}
		}

		/// <summary>
		/// 获取指定物体
		/// </summary>
		/// <returns>The item.</returns>
		/// <param name="dataIndex">Data index.</param>
		public GameObject GetItem(int dataIndex) {
			if (listItem.Count > dataIndex) {
				return listItem [dataIndex].gameObject;
			}

			return null;
		}
		#endregion  

		void Awake()  
		{  
			//Debug.Log ("Awake TableView");
			scrollRect = gameObject.GetComponent<ScrollRect>();  
			if (content == null) {
				content = transform.Find ("Viewport/Content").GetComponent<RectTransform>();  
			}
			Vector2 size = GetComponent<RectTransform> ().sizeDelta; 
			content.GetComponent<RectTransform> ().sizeDelta = size;
		} 

		void OnDestroy()  
		{  
		}  
	}  
}