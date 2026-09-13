using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class ResourceResolver : MonoBehaviour
{
	[Serializable]
	public class Binding
	{
		[SerializeField]
		private Component m_target;

		[SerializeField]
		private string m_propertyName;

		[SerializeField]
		private string m_source;

		[SerializeField]
		private string m_key;

		public Component Target => m_target;

		public string PropertyName => m_propertyName;

		public string Source => m_source;

		public string Key => m_key;
	}

	[SerializeField]
	private List<Binding> m_bindings;

	private void Awake()
	{
		Resolve();
	}

	public void Resolve()
	{
		foreach (Binding binding in m_bindings)
		{
			PropertyInfo property = binding.Target.GetType().GetProperty(binding.PropertyName);
			MethodInfo setMethod = property.GetSetMethod();
			if (string.IsNullOrEmpty(binding.Source))
			{
				object obj = INUnity.LoadObject(property.PropertyType.Name, binding.Key);
				setMethod.Invoke(binding.Target, new object[1] { obj });
			}
		}
	}
}
