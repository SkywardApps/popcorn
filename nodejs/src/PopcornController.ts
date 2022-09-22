import { OutgoingHttpHeaders } from 'http2';
import { BaseHttpController, HttpContext } from 'inversify-express-utils';
import { expand, isPropertyRequested } from './Internals';
import { IPropertyReferenceMap, IPropertyReference } from './PopcornDecorator';

export interface IPopcornControllerInternals {
	requestHttpContext() : HttpContext;
	setRequestedProperties(value: IPropertyReferenceMap) : void;
	setDefaultProperties(value: IPropertyReferenceMap) : IPropertyReferenceMap;
}

export class PopcornController extends BaseHttpController 
{
	private requestedProperties: IPropertyReferenceMap = PopcornController.DefaultProperties;
	private defaultProperties: IPropertyReferenceMap = PopcornController.DefaultProperties;

	// These items implement the private interface IPopcornControllerInternals
	private requestHttpContext() { return this.httpContext; }
	private setRequestedProperties(value: IPropertyReferenceMap) { this.requestedProperties = value; }
	private setDefaultProperties(value: IPropertyReferenceMap) { this.defaultProperties = value; }

	protected getRequestedProperties() { this.requestedProperties; }

	public isPropertyRequested(property: string): { requested: IPropertyReference; default: IPropertyReference; } | undefined 
	{
		const propertyPath = property.split('.');
		return isPropertyRequested(propertyPath,
			{
				requested: { name: '', optional: false, children: this.requestedProperties },
				default: { name: '', optional: false, children: this.defaultProperties }
			});
	}	

	protected constructor() 
	{
		super();
	}

	protected popcorn<T>(data: T, headers?: OutgoingHttpHeaders, status?: number): T 
	{
		const expanded = expand(data, this.requestedProperties, this.defaultProperties) as any;
		expanded.__headers = headers;
		expanded.__popcorn = true;
		expanded.__status = status;
		return expanded;
	}

	public static readonly DefaultProperties: IPropertyReferenceMap = {
		'!default': { name: '!default', optional: false, children: {} }
	};

	public static readonly DefaultProperty: IPropertyReference = {
		name: '!default',
		optional: true,
		children: this.DefaultProperties
	};

	public static readonly AllProperties: IPropertyReferenceMap = {
		'!all': { name: '!all', optional: false, children: {} }
	};

	public static readonly AllProperty: IPropertyReference = {
		name: '!all',
		optional: true,
		children: this.DefaultProperties
	};

	public static readonly AllDeepProperty: IPropertyReference = {
		name: '!all',
		optional: true,
		children: this.AllProperties
	};
}


