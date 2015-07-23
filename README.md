# GraphiteSharp

A simple C# lib for talking to the Graphite Stack. 

### Features
	* Udp and Tcp Client
	* Async/Await Functions.

## TODO	
	* ~~Async/Await Functions~~ DONE
	* Sanitize Metric Names. (' ' => '_', ['/','\',..] => '.'
	* Validate Valuetypes, short, int, long, float, ... aka not string or class.
	* Turn Utc Conversion on/off.
	* Document classes and functions.
	* Create usage documentation.
	* Cache Reflect based value to string converter.
	* GraphiteWebClient to wrap render url api.	
	
## License

See the [LICENSE](LICENSE.md) file for license rights and limitations (MIT)