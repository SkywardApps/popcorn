import { IPropertyReferenceMap, IPropertyReference } from './PopcornDecorator';
import { PopcornController } from './PopcornController';

export function expand(data: any, requestedProperties: IPropertyReferenceMap, defaultProperties: IPropertyReferenceMap) : any
{
	if(data === undefined || data === null)
	{
		return data;
	}

	if(Array.isArray(data))
	{
		return data.map(v => expand(v, requestedProperties, defaultProperties));
	}

	if (typeof data === 'object') 
	{
		const expandedObject: any = {};
		for (const property of Object.keys(data)) 
		{
			const cursor = isPropertyRequested([String(property)],
				{
					requested: { name: '', optional: false, children: requestedProperties },
					default: { name: '', optional: false, children: defaultProperties }
				});

			// If the property is explicitly requested, expand it with any child requests
			if (cursor) 
			{
				expandedObject[property] = expand(
					data[property],
					cursor.requested.children,
					// We need to handle when default is 'all' here as well
					cursor.default.children
				); // TODO sort out better types here
			}
		}
		return expandedObject;
	}

	return data;
}

export function parseIncludeString(includes: string): IPropertyReferenceMap 
{
	const stack: IPropertyReference[] = [];
	stack.push({ name: '', optional: false, children: {} });
	for (const c of includes) 
	{
		switch (c) 
		{
		// Starting a child list, so add a new item on the stack
		case '[':
			stack.push({ name: '', optional: false, children: {} });
			break;
			// finised a reference, add it as a child and start a new peer
		case ',':
			addNode();
			stack.push({ name: '', optional: false, children: {} });
			break;
			// Completed a child list, so add the last item as a child and pop up the stack
		case ']':
			addNode();
			break;
			// building a property name
		default:
			stack[stack.length - 1].name += c;
			break;
		}
	}

	return stack.pop()!.children;

	function addNode() 
	{
		const child = stack.pop()!;
		let propName = child.name.trim();
		if (propName.length) 
		{
			if (propName[0] == '?') 
			{
				child.optional = true;
				propName = propName.substring(1);
			}
			child.name = propName;
			// If no children were requested, make this an explicit request for defaults
			if (Object.keys(child.children).length == 0) 
			{
				child.children = PopcornController.DefaultProperties;
			}
			stack[stack.length - 1].children[propName] = child;
		}
	}
}

export function isPropertyRequested(propertyPath: string[], includeCursor: { requested: IPropertyReference; default: IPropertyReference; }) 
{
	for (const propertyCursor of propertyPath) 
	{
		if (includeCursor.requested.children[propertyCursor]) 
		{
			includeCursor = {
				requested: includeCursor.requested.children[propertyCursor],
				default: includeCursor.default.children[propertyCursor]
					?? includeCursor.default.children['!all']
					? PopcornController.AllDeepProperty
					: PopcornController.DefaultProperty,
			};
		}

		// Otherwise if everything was requested, expand it with default child requests
		else if (includeCursor.requested.children['!all']) 
		{
			includeCursor = {
				requested: PopcornController.AllProperty,
				default: includeCursor.default.children[propertyCursor]
					?? includeCursor.default.children['!all']
					? PopcornController.AllDeepProperty
					: PopcornController.DefaultProperty,
			};
		}

		// If defaults were requested and it is default expanded, expand it
		else if (includeCursor.requested.children['!default']
			&& (includeCursor.default.children[propertyCursor] || includeCursor.default.children['!all'])) 
		{
			includeCursor = {
				requested: PopcornController.DefaultProperty,
				default: includeCursor.default.children[propertyCursor]
					?? includeCursor.default.children['!all']
					? PopcornController.AllDeepProperty
					: PopcornController.DefaultProperty,
			};
		}

		else 
		{
			return undefined;
		}
	}
	return includeCursor;
}
