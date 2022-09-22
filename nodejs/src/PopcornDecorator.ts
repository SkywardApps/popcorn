import { HttpResponseMessage, JsonContent } from 'inversify-express-utils';
import { IPopcornControllerInternals, PopcornController } from './PopcornController';
import { parseIncludeString } from './Internals';

export interface IPropertyReference 
{
	name: string;
	children: IPropertyReferenceMap;
	optional: boolean;
}

export interface IPropertyReferenceMap 
{
	[key: string]: IPropertyReference;
}

export function popcorn(includes: string | IPropertyReferenceMap) 
{
	const defaultIncludes = typeof includes === 'string' ? parseIncludeString(includes) : includes;

	return (target: any, propertyName: string, descriptor: TypedPropertyDescriptor<Function>) => 
	{
		const method = descriptor.value!;
		if (!(target instanceof PopcornController)) 
		{
			throw new Error(`Target ${target} for attribute is not a PopcornController`);
		}

		descriptor.value = async function (...rest: any[]) 
		{
			const self = this as IPopcornControllerInternals;
			self.setDefaultProperties(defaultIncludes);

			// in some fashion, we need to inject the request 'include' parameter to the class
			const requestedIncludeStatement = self.requestHttpContext().request.query['include'];
			if (!requestedIncludeStatement?.length || requestedIncludeStatement === '[]') 
			{
				self.setRequestedProperties(PopcornController.DefaultProperties);
			}

			else 
			{
				// parse the string
				self.setRequestedProperties(parseIncludeString(String(requestedIncludeStatement)));
			}

			const content = await method.apply(this, rest);
			const { __headers, __popcorn, __status, ...payload } = content;
			if (!__popcorn) 
			{
				return content;
			}

			const response = new HttpResponseMessage(__status || 200);
			response.content = new JsonContent(payload);
			response.headers = {
				...response.headers,
				...__headers
			};
			return response;
		};
	};
}
